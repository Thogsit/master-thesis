using System.ComponentModel.DataAnnotations.Schema;
using SealedFga.Attributes;
using SealedFga.AuthModel;
using SealedFga.Models;
using SealedFga.Sample.Secret;

namespace SealedFga.Sample.User;

[SealedFgaTypeId("user", SealedFgaTypeIdType.String)]
public partial class UserEntityId;

public class UserEntity : ISealedFgaType<UserEntityId> {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public UserEntityId Id { get; set; } = null!;

    [SealedFgaRelation(nameof(AgencyEntityIdGroups.Member), SealedFgaRelationTargetType.User)]
    public required AgencyEntityId AgencyId { get; set; }
}
