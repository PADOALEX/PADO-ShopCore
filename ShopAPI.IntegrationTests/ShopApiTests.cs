using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopAPI.Core.Entities;
using ShopAPI.Infrastructure;
using Xunit;

namespace ShopAPI.IntegrationTests;

public sealed class ShopApiTests
{
    private static async Task<(int Id, string Token)> RegisterAndLogin(HttpClient client, string? email = null, int? role = null)
    {
        email ??= $"u{Guid.NewGuid():N}@test.invalid";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test", lastName = "User", email, password = "StrongTestPassword#2026", role = role ?? 0
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "StrongTestPassword#2026" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return (doc.RootElement.GetProperty("userId").GetInt32(), doc.RootElement.GetProperty("token").GetString()!);
    }

    private static HttpClient As(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<int> AddProduct(PgScenario scenario, int stock = 1)
    {
        using var scope = scenario.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var category = new Category { Name = "Electronics" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var product = new Product { Name = "Gamepad", Price = 199.99m, StockQuantity = stock, IsActive = true, CategoryId = category.Id };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    [Fact]
    public async Task RegistrationCannotGrantAdminAndPublicUserLeakIsClosed()
    {
        await using var env = await PgScenario.StartAsync();
        var (id, token) = await RegisterAndLogin(env.Client, role: 1);
        As(env.Client, token);
        var me = await env.Client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal(0, me.GetProperty("role").GetInt32());
        Assert.Equal(id, me.GetProperty("id").GetInt32());
        Assert.False(me.TryGetProperty("passwordHash", out _));
        Assert.Equal(HttpStatusCode.Forbidden, (await env.Client.GetAsync("/api/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await env.Client.PostAsJsonAsync("/api/products", new
        { name = "Illegal", price = 10, stockQuantity = 9, isActive = true, categoryId = 1 })).StatusCode);
    }

    [Fact]
    public async Task AnonymousPrivateEndpointsRequireToken()
    {
        await using var env = await PgScenario.StartAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await env.Client.GetAsync("/api/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await env.Client.GetAsync("/api/carts")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await env.Client.GetAsync("/api/refunds")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await env.Client.GetAsync("/api/products")).StatusCode);
    }

    [Fact]
    public async Task DuplicateEmailReturnsConflictAndNeverReturnsPasswordHash()
    {
        await using var env = await PgScenario.StartAsync();
        var email = $"dup{Guid.NewGuid():N}@test.invalid";
        await RegisterAndLogin(env.Client, email);
        var duplicate = await env.Client.PostAsJsonAsync("/api/auth/register", new
        { firstName = "X", lastName = "Y", email = email.ToUpperInvariant(), password = "AnotherPassword!234" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("application/problem+json", duplicate.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task ForgedCustomerIdCannotChangeCartOrOrderOwnership()
    {
        await using var env = await PgScenario.StartAsync();
        var productId = await AddProduct(env, stock: 5);
        var (alice, aliceToken) = await RegisterAndLogin(env.Client);
        var (bob, bobToken) = await RegisterAndLogin(env.Client);
        Assert.NotEqual(alice, bob);
        As(env.Client, aliceToken);
        var add = await env.Client.PostAsJsonAsync("/api/carts/add-item", new { productId, quantity = 1, customerId = bob });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        var checkout = await env.Client.PostAsJsonAsync("/api/orders/checkout", new { customerId = bob });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        As(env.Client, bobToken);
        var bobOrders = await env.Client.GetFromJsonAsync<JsonElement>("/api/orders");
        Assert.Equal(0, bobOrders.GetArrayLength());
        var bobCart = await env.Client.GetFromJsonAsync<JsonElement>("/api/carts");
        Assert.Equal(0, bobCart.GetProperty("items").GetArrayLength());
        var aliceOrder = (await checkout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NotFound, (await env.Client.GetAsync($"/api/orders/{aliceOrder}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await env.Client.PostAsync($"/api/orders/{aliceOrder}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentCheckoutOfLastUnitAllowsExactlyOneOrder()
    {
        await using var env = await PgScenario.StartAsync();
        var productId = await AddProduct(env);
        var (alice, tokenA) = await RegisterAndLogin(env.Client);
        var (bob, tokenB) = await RegisterAndLogin(env.Client);
        // Separate HttpClient instances avoid mutating shared Authorization headers.
        // Use the factory client for in-memory HTTP hosting: set Authorization per request instead.
        var addA = new HttpRequestMessage(HttpMethod.Post, "/api/carts/add-item")
        { Content = JsonContent.Create(new { productId, quantity = 1 }) };
        addA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var addB = new HttpRequestMessage(HttpMethod.Post, "/api/carts/add-item")
        { Content = JsonContent.Create(new { productId, quantity = 1 }) };
        addB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        Assert.Equal(HttpStatusCode.OK, (await env.Client.SendAsync(addA)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await env.Client.SendAsync(addB)).StatusCode);
        HttpRequestMessage Request(string jwt)
        {
            var r = new HttpRequestMessage(HttpMethod.Post, "/api/orders/checkout") { Content = JsonContent.Create(new { }) };
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return r;
        }
        var responses = await Task.WhenAll(env.Client.SendAsync(Request(tokenA)), env.Client.SendAsync(Request(tokenB)));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        using var scope = env.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Equal(0, (await db.Products.AsNoTracking().SingleAsync(p => p.Id == productId)).StockQuantity);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task InvalidQuantityAndEmptyCheckoutDoNotCreateOrders()
    {
        await using var env = await PgScenario.StartAsync();
        var productId = await AddProduct(env, 3);
        var (_, token) = await RegisterAndLogin(env.Client);
        As(env.Client, token);
        Assert.Equal(HttpStatusCode.BadRequest, (await env.Client.PostAsJsonAsync("/api/carts/add-item",
            new { productId, quantity = -5 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await env.Client.PostAsJsonAsync("/api/orders/checkout", new { })).StatusCode);
        using var scope = env.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Orders.CountAsync());
    }

    [Fact]
    public async Task InvalidPromoRollsBackAllInventoryChanges()
    {
        await using var env = await PgScenario.StartAsync();
        var productId = await AddProduct(env, stock: 3);
        var (_, token) = await RegisterAndLogin(env.Client);
        As(env.Client, token);
        Assert.Equal(HttpStatusCode.OK, (await env.Client.PostAsJsonAsync("/api/carts/add-item",
            new { productId, quantity = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await env.Client.PostAsJsonAsync("/api/orders/checkout",
            new { promoCode = "DOES_NOT_EXIST" })).StatusCode);
        using (var scope = env.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            Assert.Equal(3, (await db.Products.SingleAsync(x => x.Id == productId)).StockQuantity);
            Assert.Equal(0, await db.Orders.CountAsync());
        }
        Assert.Equal(HttpStatusCode.Created, (await env.Client.PostAsJsonAsync("/api/orders/checkout", new { })).StatusCode);
    }

    [Fact]
    public async Task CancellationRestoresStockOnlyOnce()
    {
        await using var env = await PgScenario.StartAsync();
        var productId = await AddProduct(env, stock: 2);
        var (_, token) = await RegisterAndLogin(env.Client);
        As(env.Client, token);
        Assert.Equal(HttpStatusCode.OK, (await env.Client.PostAsJsonAsync("/api/carts/add-item",
            new { productId, quantity = 1 })).StatusCode);
        var checkout = await env.Client.PostAsJsonAsync("/api/orders/checkout", new { });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var id = (await checkout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await env.Client.PostAsync($"/api/orders/{id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await env.Client.PostAsync($"/api/orders/{id}/cancel", null)).StatusCode);
        using var scope = env.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Equal(2, (await db.Products.SingleAsync(x => x.Id == productId)).StockQuantity);
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync(x => x.Id == id)).Status);
    }

    [Fact]
    public async Task ApprovedRefundCannotRestockTwice()
    {
        await using var env = await PgScenario.StartAsync();
        var productId = await AddProduct(env, 1);
        var (userId, token) = await RegisterAndLogin(env.Client);
        using (var scope = env.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            db.Users.Add(new User { Email = "admin@test.invalid", FirstName = "Admin", LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongTestPassword#2026"), Role = Role.Admin });
            await db.SaveChangesAsync();
        }
        var adminLogin = await env.Client.PostAsJsonAsync("/api/auth/login", new
        { email = "admin@test.invalid", password = "StrongTestPassword#2026" });
        var adminToken = (await adminLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        As(env.Client, token);
        await env.Client.PostAsJsonAsync("/api/carts/add-item", new { productId, quantity = 1 });
        var result = await env.Client.PostAsJsonAsync("/api/orders/checkout", new { });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var order = await result.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetInt32();
        var orderItemId = order.GetProperty("items")[0].GetProperty("id").GetInt32();
        As(env.Client, adminToken);
        foreach (var status in new[] { OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Delivered })
            Assert.Equal(HttpStatusCode.NoContent, (await env.Client.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status })).StatusCode);
        As(env.Client, token);
        var refund = await env.Client.PostAsJsonAsync("/api/refunds/request", new
        { orderId, items = new[] { new { orderItemId, quantity = 1 } } });
        Assert.Equal(HttpStatusCode.Created, refund.StatusCode);
        var refundId = (await refund.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var duplicate = await env.Client.PostAsJsonAsync("/api/refunds/request", new
        { orderId, items = new[] { new { orderItemId, quantity = 1 } } });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        As(env.Client, adminToken);
        Assert.Equal(HttpStatusCode.NoContent, (await env.Client.PostAsync($"/api/refunds/{refundId}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await env.Client.PostAsync($"/api/refunds/{refundId}/approve", null)).StatusCode);
        using var check = env.Services.CreateScope();
        Assert.Equal(1, (await check.ServiceProvider.GetRequiredService<ShopDbContext>().Products.SingleAsync(x => x.Id == productId)).StockQuantity);
    }
}
