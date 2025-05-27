using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SealedFga.Sample.Database;

namespace SealedFga.Sample.Secret;

public class SecretService(SealedFgaSampleContext context) : ISecretService
{
    public async Task<List<SecretEntity>> GetAllSecretsAsync()
    {
        return await context.SecretEntities.ToListAsync();
    }

    public async Task<SecretEntity?> GetSecretByIdAsync(SecretEntityId secretId)
    {
        return await context.SecretEntities.FindAsync(secretId);
    }

    public void DependencyInjectionTest()
    {
        SomeMethodWithUniqueName();
    }

    private void SomeMethodWithUniqueName()
    {
    }
}