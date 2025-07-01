using System.ComponentModel.DataAnnotations.Schema;
using SealedFga.Attributes;
using SealedFga.AuthModel;
using SealedFga.Models;

namespace SealedFga.Sample.Secret;

[OpenFgaTypeId("agency", OpenFgaTypeIdType.Guid)]
public partial class AgencyEntityId;

public class AgencyEntity : IOpenFgaType<AgencyEntityId> {
    public required string Name { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public AgencyEntityId Id { get; set; } = null!;
}
