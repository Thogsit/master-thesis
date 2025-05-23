using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SealedFga.Sample.Database;
using SealedFga.Sample.FgaAuthorization;

namespace SealedFga.Sample.Secret;

public record UpdateSecretRequestDto(string Value);

[ApiController]
[Route("secrets")]
public class SecretController(SealedFgaSampleContext context, SecretService secretService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllSecrets(
        [FgaAuthorizeList(Relation = nameof(SecretEntityIdAttributes.can_view))]
        List<SecretEntity> secrets
    )
    {
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

    [HttpPut("{secretId}")]
    public async Task<IActionResult> UpdateSecretById(
        [FromRoute] SecretEntityId secretId,
        [FgaAuthorize(Relation = nameof(SecretEntityIdAttributes.can_edit), ParameterName = nameof(secretId))]
        SecretEntity secret,
        [FromBody] UpdateSecretRequestDto updateSecretRequestDto
    )
    {
        secret.Value = updateSecretRequestDto.Value;
        await context.SaveChangesAsync();

        return Ok(secret);
    }
}