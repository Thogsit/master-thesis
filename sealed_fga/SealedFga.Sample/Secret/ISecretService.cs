using System.Collections.Generic;
using System.Threading.Tasks;
using SealedFga.Attributes;

namespace SealedFga.Sample.Secret;

[ImplementedBy(typeof(SecretService))]
public interface ISecretService {
    Task<List<SecretEntity>> GetAllSecretsAsync();
    Task<SecretEntity?> GetSecretByIdAsync(SecretEntityId secretId);
}
