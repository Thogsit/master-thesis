using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using OpenFga.Sdk.Model;
using SealedFga.Attributes;
using SealedFga.Exceptions;
using SealedFga.Fga;
using SealedFga.Generators.AuthModel;
using SealedFga.Util;

namespace SealedFga.ModelBinder;

/// <summary>
///     Model binder for binding FGA entity parameters annotated with <see cref="FgaAuthorizeAttribute" />.
/// </summary>
public class SealedFgaEntityModelBinder(Type dbContextType)
    : SealedFgaModelBinder<FgaAuthorizeAttribute>(dbContextType) {
    /// <summary>
    ///     Used to bind FGA entity parameters that have been annotated with the <see cref="FgaAuthorizeAttribute" />.
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
    /// <param name="sealedFgaService">The SealedFGA service.</param>
    /// <param name="openFgaRawUserString">
    ///     The raw OpenFGA user string from the HttpContext's User ClaimsPrincipal.
    /// </param>
    /// <param name="attr">The parameter's annotation.</param>
    protected override async Task FgaBind(
        ModelBindingContext context,
        ControllerParameterDescriptor param,
        SealedFgaService sealedFgaService,
        string openFgaRawUserString,
        FgaAuthorizeAttribute attr
    ) {
        // Get the value of the parameter specified in ParameterName, e.g. "15ff5687-3f4d-4cae-8a19-68670e5cdff2"
        var parameterVal = (string?) context.ActionContext.RouteData.Values[attr.ParameterName];
        if (parameterVal == null) {
            return;
        }

        // Get the ID type of the ID parameter, e.g. "SecretEntityId"
        var idType = context.ActionContext.ActionDescriptor
                            .Parameters
                            .OfType<ControllerParameterDescriptor>()
                            .FirstOrDefault(p => p.Name == attr.ParameterName);
        if (idType == null) {
            return;
        }

        // Convert "raw" string ID into strongly typed ID, e.g. "15ff5687-3f4d-4cae-8a19-68670e5cdff2" -> SecretEntityId(...)
        var parseMethod = idType.ParameterInfo.ParameterType.GetMethod(TypeNameIdGenerator.ParseMethodName);
        if (parseMethod == null) {
            return;
        }

        var idVal = parseMethod.Invoke(null, [parameterVal]);
        if (idVal == null) {
            return;
        }

        var parsedId = IdUtil.ParseId(idType.ParameterInfo.ParameterType, parameterVal);

        // Retrieve the OpenFGA ID tuple string from the ID object, e.g. SecretEntityId(...) -> "secret:15ff5687-3f4d-4cae-8a19-68670e5cdff2"
        var openFgaIdTupleStringMethod =
            idType.ParameterInfo.ParameterType.GetMethod(TypeNameIdGenerator.OpenFgaIdTupleStringMethodName);
        var openFgaRawObjectString = (string?) openFgaIdTupleStringMethod?.Invoke(parsedId, null);
        if (openFgaRawObjectString == null) {
            return;
        }

        // Check if the user has the required permission
        var isAuthorized = await sealedFgaService.CheckAsync(
            new TupleKey {
                User = openFgaRawUserString,
                Relation = attr.Relation,
                Object = openFgaRawObjectString,
            }
        );

        if (!isAuthorized) {
            throw new FgaAuthorizationException(openFgaRawObjectString);
        }

        // Find Set<T>().FindAsync method using reflection
        var dbContext = GetDbContext(context);
        var entityType = context.ModelType;
        var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
                                        ?.MakeGenericMethod(entityType);
        if (setMethod == null) {
            return;
        }

        var dbSet = setMethod.Invoke(dbContext, null);
        var findAsyncMethod = dbSet.GetType().GetMethod(nameof(DbSet<>.FindAsync), [typeof(object[])]);

        var findTask = findAsyncMethod!.Invoke(dbSet, [new[] { parsedId }]);
        if (findTask == null) {
            return;
        }

        // Convert the returned ValueTask to Task and await it
        var asTaskMethod = findTask.GetType().GetMethod("AsTask");
        if (asTaskMethod != null) {
            var task = (Task) asTaskMethod.Invoke(findTask, null);
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            var entity = resultProperty?.GetValue(task);

            if (entity != null) {
                context.Result = ModelBindingResult.Success(entity);
            } else {
                throw new FgaEntityNotFoundException(context.ModelType, parsedId);
            }
        } else {
            // Fallback: use reflection to get the Result property directly
            var resultProperty = findTask.GetType().GetProperty("Result");
            if (resultProperty != null) {
                var entity = resultProperty.GetValue(findTask);
                if (entity != null) {
                    context.Result = ModelBindingResult.Success(entity);
                } else {
                    throw new FgaEntityNotFoundException(context.ModelType, parsedId);
                }
            }
        }
    }
}
