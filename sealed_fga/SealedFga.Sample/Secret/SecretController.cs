using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SealedFga.Sample.FgaAuthorization;

namespace SealedFga.Sample.Secret;

[ApiController]
[Route("secrets")]
public class SecretController(SecretService secretService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllSecrets()
    {
        var secrets = await secretService.GetAllSecretsAsync();
        return Ok(secrets);
    }

    [HttpGet("{secretId}")]
    public async Task<IActionResult> GetSecretById(
        [FromRoute] SecretEntityId secretId,
        [FgaAuthorize(Relation = nameof(SecretEntityIdAttributes.can_view), ParameterName = nameof(secretId))]
        SecretEntity secret
    )
    {
        return Ok(secret);
    }
}