using Microsoft.EntityFrameworkCore;
using ShopAPI.Core.Entities;
using ShopAPI.Infrastructure;

namespace ShopAPI.Web.Infrastructure;

public static class DemoSeeder
{
    public static async Task SeedAsync(ShopDbContext db, CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct) || await db.Products.AnyAsync(ct))
            throw new InvalidOperationException("Catalog is not empty. Demo seed was refused to avoid overwriting records.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var categories = new List<Category>
        {
            new Category { Name = "Смартфони" },
            new Category { Name = "Ноутбуки" },
            new Category { Name = "Аудіо" },
            new Category { Name = "Геймінг" },
            new Category { Name = "Аксесуари" },
            new Category { Name = "Фото та відео" },
            new Category { Name = "Монітори та ТВ" },
            new Category { Name = "Розумний дім" },
            new Category { Name = "Носима електроніка" },
            new Category { Name = "Зарядні пристрої" },
        };

        await db.Categories.AddRangeAsync(categories, ct);
        await db.SaveChangesAsync(ct);

        var products = new List<Product>
        {
            new Product { Name = "iPhone 15 Pro Max 256GB",        Price = 55000,  StockQuantity = 20, IsActive = true, CategoryId = categories[0].Id },
            new Product { Name = "Samsung Galaxy S24 Ultra",       Price = 52000,  StockQuantity = 15, IsActive = true, CategoryId = categories[0].Id },
            new Product { Name = "Google Pixel 8 Pro",             Price = 42000,  StockQuantity = 10, IsActive = true, CategoryId = categories[0].Id },
            new Product { Name = "Xiaomi 14 Pro",                  Price = 35000,  StockQuantity = 25, IsActive = true, CategoryId = categories[0].Id },
            new Product { Name = "Nothing Phone (2)",              Price = 28000,  StockQuantity = 8,  IsActive = true, CategoryId = categories[0].Id },

            new Product { Name = "MacBook Pro 16 M3 Max",          Price = 160000, StockQuantity = 5,  IsActive = true, CategoryId = categories[1].Id },
            new Product { Name = "MacBook Air 15 M2",              Price = 65000,  StockQuantity = 12, IsActive = true, CategoryId = categories[1].Id },
            new Product { Name = "ASUS ROG Strix G16",             Price = 75000,  StockQuantity = 7,  IsActive = true, CategoryId = categories[1].Id },
            new Product { Name = "Lenovo ThinkPad X1 Carbon",      Price = 80000,  StockQuantity = 10, IsActive = true, CategoryId = categories[1].Id },
            new Product { Name = "Dell XPS 15",                    Price = 85000,  StockQuantity = 6,  IsActive = true, CategoryId = categories[1].Id },

            new Product { Name = "AirPods Pro 2",                  Price = 11000,  StockQuantity = 50, IsActive = true, CategoryId = categories[2].Id },
            new Product { Name = "Sony WH-1000XM5",                Price = 14000,  StockQuantity = 15, IsActive = true, CategoryId = categories[2].Id },
            new Product { Name = "Marshall Motif II ANC",           Price = 8500,   StockQuantity = 20, IsActive = true, CategoryId = categories[2].Id },
            new Product { Name = "JBL Flip 6",                     Price = 4500,   StockQuantity = 40, IsActive = true, CategoryId = categories[2].Id },
            new Product { Name = "Sonos Era 300",                  Price = 18000,  StockQuantity = 8,  IsActive = true, CategoryId = categories[2].Id },

            new Product { Name = "PlayStation 5 Slim",             Price = 22000,  StockQuantity = 30, IsActive = true, CategoryId = categories[3].Id },
            new Product { Name = "Xbox Series X",                  Price = 21000,  StockQuantity = 15, IsActive = true, CategoryId = categories[3].Id },
            new Product { Name = "Nintendo Switch OLED",           Price = 14000,  StockQuantity = 25, IsActive = true, CategoryId = categories[3].Id },
            new Product { Name = "Steam Deck OLED 1TB",            Price = 29000,  StockQuantity = 10, IsActive = true, CategoryId = categories[3].Id },
            new Product { Name = "Razer BlackShark V2 Pro",        Price = 9500,   StockQuantity = 18, IsActive = true, CategoryId = categories[3].Id },

            new Product { Name = "Logitech MX Master 3S",          Price = 4200,   StockQuantity = 30, IsActive = true, CategoryId = categories[4].Id },
            new Product { Name = "Keychron K8 Pro",                Price = 5500,   StockQuantity = 15, IsActive = true, CategoryId = categories[4].Id },
            new Product { Name = "Apple Magic Keyboard",           Price = 6000,   StockQuantity = 20, IsActive = true, CategoryId = categories[4].Id },
            new Product { Name = "Razer DeathAdder V3 Pro",        Price = 6500,   StockQuantity = 12, IsActive = true, CategoryId = categories[4].Id },
            new Product { Name = "Elgato Stream Deck MK.2",        Price = 7200,   StockQuantity = 10, IsActive = true, CategoryId = categories[4].Id },

            new Product { Name = "Sony Alpha A7 IV Body",          Price = 115000, StockQuantity = 5,  IsActive = true, CategoryId = categories[5].Id },
            new Product { Name = "Canon EOS R6 Mark II",           Price = 105000, StockQuantity = 4,  IsActive = true, CategoryId = categories[5].Id },
            new Product { Name = "DJI Osmo Pocket 3",              Price = 18500,  StockQuantity = 12, IsActive = true, CategoryId = categories[5].Id },
            new Product { Name = "GoPro Hero 12 Black",            Price = 16000,  StockQuantity = 20, IsActive = true, CategoryId = categories[5].Id },
            new Product { Name = "Insta360 X4",                    Price = 21000,  StockQuantity = 8,  IsActive = true, CategoryId = categories[5].Id },

            new Product { Name = "LG OLED C3 55\"",                Price = 55000,  StockQuantity = 6,  IsActive = true, CategoryId = categories[6].Id },
            new Product { Name = "Samsung Odyssey G7 27\"",        Price = 22000,  StockQuantity = 10, IsActive = true, CategoryId = categories[6].Id },
            new Product { Name = "Dell UltraSharp U2723D 27\"",    Price = 28000,  StockQuantity = 8,  IsActive = true, CategoryId = categories[6].Id },
            new Product { Name = "LG UltraWide 34WP85C 34\"",      Price = 32000,  StockQuantity = 5,  IsActive = true, CategoryId = categories[6].Id },
            new Product { Name = "Apple Studio Display",           Price = 75000,  StockQuantity = 3,  IsActive = true, CategoryId = categories[6].Id },

            new Product { Name = "Apple HomePod 2",                Price = 13000,  StockQuantity = 15, IsActive = true, CategoryId = categories[7].Id },
            new Product { Name = "Google Nest Hub Max",            Price = 9500,   StockQuantity = 12, IsActive = true, CategoryId = categories[7].Id },
            new Product { Name = "Philips Hue Starter Kit",        Price = 5500,   StockQuantity = 25, IsActive = true, CategoryId = categories[7].Id },
            new Product { Name = "Aqara Hub M2",                   Price = 2200,   StockQuantity = 30, IsActive = true, CategoryId = categories[7].Id },
            new Product { Name = "Roborock S8 Pro Ultra",          Price = 38000,  StockQuantity = 7,  IsActive = true, CategoryId = categories[7].Id },

            new Product { Name = "Apple Watch Series 9 45mm",     Price = 16500,  StockQuantity = 25, IsActive = true, CategoryId = categories[8].Id },
            new Product { Name = "Samsung Galaxy Watch 6",         Price = 10500,  StockQuantity = 20, IsActive = true, CategoryId = categories[8].Id },
            new Product { Name = "Garmin Fenix 7X Pro",            Price = 35000,  StockQuantity = 8,  IsActive = true, CategoryId = categories[8].Id },
            new Product { Name = "Xiaomi Smart Band 8 Pro",        Price = 2800,   StockQuantity = 50, IsActive = true, CategoryId = categories[8].Id },
            new Product { Name = "Oura Ring Gen 3",                Price = 12000,  StockQuantity = 10, IsActive = true, CategoryId = categories[8].Id },

            new Product { Name = "Anker 747 GaNPrime 150W",        Price = 4200,   StockQuantity = 30, IsActive = true, CategoryId = categories[9].Id },
            new Product { Name = "Apple MagSafe Charger 15W",      Price = 1800,   StockQuantity = 60, IsActive = true, CategoryId = categories[9].Id },
            new Product { Name = "Baseus 65W Power Bank 20000mAh", Price = 3500,   StockQuantity = 35, IsActive = true, CategoryId = categories[9].Id },
            new Product { Name = "Belkin 3-in-1 Wireless Pad",     Price = 3200,   StockQuantity = 20, IsActive = true, CategoryId = categories[9].Id },
            new Product { Name = "Anker 737 PowerBank 24000mAh",   Price = 6500,   StockQuantity = 15, IsActive = true, CategoryId = categories[9].Id },
        };

        await db.Products.AddRangeAsync(products, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
