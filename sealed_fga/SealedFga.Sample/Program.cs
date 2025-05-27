using SealedFga.Sample.Database;
using SealedFga.Sample.Secret;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SealedFga.ModelBinding;

namespace SealedFga.Sample;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddDbContext<SealedFgaSampleContext>(options =>
            options.UseInMemoryDatabase("SealedFgaSampleDb"));
        builder.Services.AddScoped<ISecretService, SecretService>();
        builder.Services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new SealedFgaModelBinderProvider<SealedFgaSampleContext>());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseRouting();
        app.MapControllers();

        // Seed the database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SealedFgaSampleContext>();
            context.Database.EnsureCreated();
        }

        app.Run();
    }
}
