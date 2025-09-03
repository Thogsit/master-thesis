using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client;
using SealedFga.Middleware;
using SealedFga.Sample.Auth;
using SealedFga.Sample.Database;
using SealedFga.Sample.Secret;

namespace SealedFga.Sample;

public static class Program {
    public static async Task Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddDbContext<SealedFgaSampleContext>((sp, options) => {
                options.UseInMemoryDatabase("SealedFgaSampleDb");
                options.AddSealedFga(sp);
            }
        );
        builder.Services.AddScoped<ISecretService, SecretService>();

        // Add authentication
        builder.Services.AddAuthentication("MockScheme")
               .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("MockScheme", options => { });
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton<OpenFgaClient>(_ => {
                var fgaClient = new OpenFgaClient(
                    new ClientConfiguration {
                        ApiUrl = "http://localhost:8080",
                    }
                );
                var storeId = fgaClient.ListStores(null).Result.Stores[0].Id;
                fgaClient.Dispose();
                fgaClient = new OpenFgaClient(
                    new ClientConfiguration {
                        ApiUrl = "http://localhost:8080",
                        StoreId = storeId,
                    }
                );
                var authModelId = fgaClient.ReadAuthorizationModels().Result.AuthorizationModels[0].Id;
                fgaClient.Dispose();
                return new OpenFgaClient(
                    new ClientConfiguration {
                        ApiUrl = "http://localhost:8080",
                        StoreId = storeId,
                        AuthorizationModelId = authModelId,
                    }
                );
            }
        );

        // Configure SealedFGA
        builder.Services.ConfigureSealedFga<SealedFgaSampleContext>(
            opt => opt.QueueFgaServiceOperations = false,
            false
        );

        var app = builder.Build();

        // TODO: Maybe move SealedFGA related config into some init method
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRouting();
        app.MapControllers();
        app.UseSealedFga();

        // Seed the database
        using (var scope = app.Services.CreateScope()) {
            var context = scope.ServiceProvider.GetRequiredService<SealedFgaSampleContext>();
            await context.Database.EnsureCreatedAsync();
            await context.AddDummyData();
        }

        await app.RunAsync();
    }
}
