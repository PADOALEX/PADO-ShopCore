using System.Text;

namespace ShopAPI.Web.Infrastructure;

// Validate secrets once the real host starts, not during early Program configuration.
// WebApplicationFactory can then safely supply isolated test credentials.
internal sealed class StartupConfigurationValidator(IConfiguration configuration) : IHostedService
{
    public static void Validate(IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
            string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) ||
            string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            throw new InvalidOperationException(
                "Set Jwt__Key (at least 32 UTF-8 bytes), Jwt__Issuer and Jwt__Audience in secure configuration.");
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
            throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection in secure configuration.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Validate(configuration);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
