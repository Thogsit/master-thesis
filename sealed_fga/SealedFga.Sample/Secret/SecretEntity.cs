using System.ComponentModel.DataAnnotations.Schema;
using SealedFga.Attributes;
using SealedFga.Models;

namespace SealedFga.Sample.Secret;

[OpenFgaTypeId("secret", OpenFgaTypeIdType.Guid)]
public partial class SecretEntityId;

public class SecretEntity : IOpenFgaType<SecretEntityId> {
    [OpenFgaRelation(nameof(SecretEntityIdGroups.OwnedBy))]
    public AgencyEntityId OwningAgencyId { get; set; } = null!;

    public required string Value { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public SecretEntityId Id { get; set; } = null!;
}
