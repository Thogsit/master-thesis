using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class SealedFgaEntityModelBinderGenerator {
    public static GeneratedFile Generate()
        => new(
            "SealedFgaEntityModelBinder.g.cs",
            """

            /// <summary>
            ///     Model binder for binding FGA entity parameters annotated with <see cref="FgaAuthorizeAttribute"/>.
            /// </summary>
            public class SealedFgaEntityModelBinder(Type dbContextType) : SealedFgaModelBinder<FgaAuthorizeAttribute>(dbContextType)
            {
                /// <summary>
                ///     Used to bind FGA entity parameters that have been annotated with the <see cref="FgaAuthorizeAttribute"/>.
                ///     Checks whether the user has the required permission given by <c>Relation</c>.
                ///     If valid, retrieves the entity from the DB and injects it into the annotated parameter.
                /// </summary>
                /// <example>
                ///     <code>
                ///     public async Task&lt;IActionResult&gt; GetSecret(
                ///         SecretEntityId secretId,
                ///         [FgaAuthorize(
                ///             Relation = nameof(SecretEntityIdAttributes.can_view),
                ///             ParameterName = nameof(secretId))
                ///         ] SecretEntity secret
                ///     );
                ///     </code>
                /// </example>
                /// <param name="context">The model binding context.</param>
                /// <param name="param">The annotated parameter.</param>
                /// <param name="attr">The parameter's annotation.</param>
                protected override async Task FgaBind(
                    ModelBindingContext context,
                    ControllerParameterDescriptor param,
                    FgaAuthorizeAttribute attr
                )
                {
                    // Get the value of the parameter specified in ParameterName, e.g. "15ff5687-3f4d-4cae-8a19-68670e5cdff2"
                    var parameterVal = (string?)context.ActionContext.RouteData.Values[attr.ParameterName];
                    if (parameterVal == null)
                    {
                        return;
                    }

                    // Get the ID type of the ID parameter, e.g. "SecretEntityId"
                    var idType = context.ActionContext.ActionDescriptor
                        .Parameters
                        .OfType<ControllerParameterDescriptor>()
                        .FirstOrDefault(p => p.Name == attr.ParameterName);
                    if (idType == null)
                    {
                        return;
                    }

                    // Convert "raw" string ID into strongly typed ID, e.g. "15ff5687-3f4d-4cae-8a19-68670e5cdff2" -> SecretEntityId
                    var parseMethod = idType.ParameterInfo.ParameterType.GetMethod(nameof(IOpenFgaTypeId<>.Parse));
                    if (parseMethod == null)
                    {
                        return;
                    }

                    var idVal = parseMethod.Invoke(null, [parameterVal]);
                    if (idVal == null)
                    {
                        return;
                    }

                    // Get the entity from DB by its ID as its primary key
                    var dbContext = GetDbContext(context);
                    var entity = await dbContext.FindAsync(context.ModelType, idVal);
                    if (entity == null)
                    {
                        return;
                    }

                    context.Result = ModelBindingResult.Success(entity);
                }
            }
            """,
            new HashSet<string>([
                    "System",
                    "System.Linq",
                    "System.Threading.Tasks",
                    "Microsoft.AspNetCore.Mvc.Controllers",
                    "Microsoft.AspNetCore.Mvc.ModelBinding",
                    "SealedFga.Attributes",
                    "SealedFga",
                ]
            ),
            Settings.ModelBindingNamespace
        );
}
