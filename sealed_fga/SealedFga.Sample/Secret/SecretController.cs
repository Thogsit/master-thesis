using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SealedFga.Attributes;
using SealedFga.Sample.Database;

namespace SealedFga.Sample.Secret;

public record UpdateSecretRequestDto(string Value);

[ApiController]
[Route("secrets")]
public class SecretController(
    SealedFgaSampleContext context,
    ISecretService secretService
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

        var someSecret = secretService.GetSecretByIdAsync(secretId);

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

        // Create a copy; should receive all checked permissions
        var newId = secretId;

        SealedFgaGuard.RequireCheck(newId, SecretEntityIdAttributes.can_view, SecretEntityIdAttributes.can_edit);
        SealedFgaGuard.RequireCheck(secret, SecretEntityIdAttributes.can_edit);
        SealedFgaGuard.RequireCheck(secretId, SecretEntityIdAttributes.can_edit, SecretEntityIdAttributes.can_view);

        secret.OwningAgencyId = newAgency.Id;
        await context.SaveChangesAsync();

        return Ok(secret);
    }
}
