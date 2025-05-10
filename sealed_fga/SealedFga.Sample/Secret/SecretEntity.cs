using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using SealedFga.Attributes;
using SealedFga.Models;

namespace SealedFga.Sample.Secret;

[OpenFgaTypeId("secret", OpenFgaTypeIdType.Guid)]
public partial class SecretEntityId;

public class SecretEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public SecretEntityId Id { get; set; }
    public required string Value { get; set; }
}
