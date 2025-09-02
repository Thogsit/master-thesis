using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client;
using SealedFga.Fga;
using SealedFga.Middleware;
using SealedFga.ModelBinder;
using SealedFga.Sample.Auth;
using SealedFga.Sample.Database;
using SealedFga.Sample.Secret;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace SealedFga.Sample;

public static class Program {
    public static void Main(string[] args) {
        SealedFgaInit.Initialize();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddDbContext<SealedFgaSampleContext>((sp, options) => {
                options.UseInMemoryDatabase("SealedFgaSampleDb");
                options.AddInterceptors(
                    sp.GetRequiredService<SealedFgaSaveChangesInterceptor>()
                );
            }
        );
        builder.Services.AddScoped<ISecretService, SecretService>();
        builder.Services.AddControllers(options => {
                options.ModelBinderProviders.Insert(0, new SealedFgaModelBinderProvider<SealedFgaSampleContext>());
            }
        );

        // Add authentication
        builder.Services.AddAuthentication("MockScheme")
               .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("MockScheme", options => { });
        builder.Services.AddAuthorization();

        // TODO: Make SealedFga configuration less manual
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
        builder.Services.AddScoped<SealedFgaService>();
        builder.Services.AddScoped<SealedFgaSaveChangesInterceptor>();
        builder.Services.AddTickerQ(opt => {
                opt.AddOperationalStore<SealedFgaSampleContext>(efOpt => {
                        efOpt.UseModelCustomizerForMigrations();
                    }
                );
                opt.AddDashboard("/tickerq");
            }
        );

        var app = builder.Build();

        app.UseSealedFgaExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRouting();
        app.MapControllers();
        app.UseTickerQ();

        // Seed the database
        using (var scope = app.Services.CreateScope()) {
            var context = scope.ServiceProvider.GetRequiredService<SealedFgaSampleContext>();
            context.Database.EnsureCreated();
        }

        app.Run();
    }
}
