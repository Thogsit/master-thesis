using System.ComponentModel.DataAnnotations.Schema;
using SealedFga.Attributes;
using SealedFga.Models;

namespace SealedFga.Sample.Secret;

[OpenFgaTypeId("agency", OpenFgaTypeIdType.Guid)]
public partial class AgencyEntityId;

public class AgencyEntity : IOpenFgaType<AgencyEntityId> {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public AgencyEntityId Id { get; set; } = null!;

    public required string Name { get; set; }
}
