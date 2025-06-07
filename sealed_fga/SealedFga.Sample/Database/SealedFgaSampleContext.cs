#pragma warning disable CS8618 // The DbSet properties get initialized by Entity Framework, so we can safely ignore the nullability warning here.

using Microsoft.EntityFrameworkCore;
using SealedFga.Sample.Secret;

namespace SealedFga.Sample.Database;

public class SealedFgaSampleContext(DbContextOptions<SealedFgaSampleContext> options) : DbContext(options) {
    public DbSet<SecretEntity> SecretEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<SecretEntity>().HasData(
            new SecretEntity {
                Id = SecretEntityId.Parse("f6c603cd-881c-4433-ae74-8a0b4e70d67b"),
                Value = "First secret",
            },
            new SecretEntity {
                Id = SecretEntityId.Parse("c4ffff35-0c6d-4d1a-847c-1dfda5fa9bd9"),
                Value = "Second secret",
            },
            new SecretEntity {
                Id = SecretEntityId.Parse("0240d59b-2d04-4391-9700-1902b75385b9"),
                Value = "Third secret",
            }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureSealedFga();
    }
}
