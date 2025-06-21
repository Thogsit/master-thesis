#pragma warning disable CS8618 // The DbSet properties get initialized by Entity Framework, so we can safely ignore the nullability warning here.

using Microsoft.EntityFrameworkCore;
using SealedFga.Sample.Secret;
using SealedFga.Sample.StateSync;

namespace SealedFga.Sample.Database;

public class SealedFgaSampleContext(DbContextOptions<SealedFgaSampleContext> options) : DbContext(options) {
    private static readonly OpenFgaSaveChangesInterceptor OpenFgaSaveChangesInterceptor = new();

    public DbSet<AgencyEntity> AgencyEntities { get; set; }

    public DbSet<SecretEntity> SecretEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        var agency1 = new AgencyEntity {
            Id = AgencyEntityId.Parse("b490657a-2c5f-4bab-b540-3753d02aca53"),
            Name = "Agency One",
        };
        var agency2 = new AgencyEntity {
            Id = AgencyEntityId.Parse("3076b12b-b17c-42ce-b3f2-af758a628048"),
            Name = "Agency Two",
        };
        modelBuilder.Entity<AgencyEntity>().HasData(agency1, agency2);
        modelBuilder.Entity<SecretEntity>().HasData(
            new SecretEntity {
                Id = SecretEntityId.Parse("f6c603cd-881c-4433-ae74-8a0b4e70d67b"),
                Value = "First secret",
                OwningAgencyId = agency1.Id,
            },
            new SecretEntity {
                Id = SecretEntityId.Parse("c4ffff35-0c6d-4d1a-847c-1dfda5fa9bd9"),
                Value = "Second secret",
                OwningAgencyId = agency1.Id,
            },
            new SecretEntity {
                Id = SecretEntityId.Parse("0240d59b-2d04-4391-9700-1902b75385b9"),
                Value = "Third secret",
                OwningAgencyId = agency1.Id,
            }
        );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(OpenFgaSaveChangesInterceptor);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureSealedFga();
    }
}
