using System.Threading.Tasks;
using SealedFga.Sample.Database;

namespace SealedFga.Sample.Secret;

public class SecretService(SealedFgaSampleContext context) : ISecretService {
    public async Task<SecretEntity?> GetSecretByIdAsync(SecretEntityId secretId) {
        SealedFgaGuard.RequireCheck(secretId, SecretEntityIdAttributes.can_view, SecretEntityIdAttributes.can_edit);
        return await context.SecretEntities.FindAsync(secretId);
    }
}
