using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SealedFga.ModelBinding;
using SealedFga.Sample.Database;

namespace SealedFga.Sample.Secret;

public record UpdateSecretRequestDto(string Value);

[ApiController]
[Route("secrets")]
public class SecretController(
    SealedFgaSampleContext context
) : ControllerBase {
    [HttpGet]
    public IActionResult GetAllSecrets(
        [FgaAuthorizeList(Relation = nameof(SecretEntityIdAttributes.can_view))]
        List<SecretEntity> secrets
    ) => Ok(secrets);

    [HttpGet("{secretId}")]
    public IActionResult GetSecretById(
        [FromRoute] SecretEntityId secretId,
        [FgaAuthorize(Relation = nameof(SecretEntityIdAttributes.can_view), ParameterName = nameof(secretId))]
        SecretEntity secret
    ) => Ok(secret);

    [HttpPut("{secretId}")]
    public async Task<IActionResult> UpdateSecretById(
        [FromRoute] SecretEntityId secretId,
        [FgaAuthorize(Relation = nameof(SecretEntityIdAttributes.can_edit), ParameterName = nameof(secretId))]
        SecretEntity secret,
        [FromBody] UpdateSecretRequestDto updateSecretRequestDto
    ) {
        secret.Value = updateSecretRequestDto.Value;
        await context.SaveChangesAsync();

        var secrets = await context.SecretEntities.ToListAsync();
        var secretsSync = context.SecretEntities.ToList();

        return Ok(secret);
    }

    [HttpPost("{secretId}/toggle-agency")]
    public async Task<IActionResult> ToggleAgency(
        [FromRoute] SecretEntityId secretId,
        [FgaAuthorize(Relation = nameof(SecretEntityIdAttributes.can_edit), ParameterName = nameof(secretId))]
        SecretEntity secret
    ) {
        var agencies = await context.AgencyEntities.ToListAsync();
        var newAgency = agencies.First(a => a.Id != secret.OwningAgencyId);

        secret.OwningAgencyId = newAgency.Id;
        await context.SaveChangesAsync();

        return Ok(secret);
    }
}
