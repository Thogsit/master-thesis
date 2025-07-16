using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SealedFga.Sample.Database;

namespace SealedFga.Sample.Secret;

public class SecretService(SealedFgaSampleContext context) : ISecretService {
    public async Task<List<SecretEntity>> GetAllSecretsAsync()
        => await context.SecretEntities.ToListAsync();

    public async Task<SecretEntity?> GetSecretByIdAsync(SecretEntityId secretId) {
        SealedFgaGuard.RequireCheck(secretId, SecretEntityIdAttributes.can_edit);
        return await context.SecretEntities.FindAsync(secretId);
    }
}
