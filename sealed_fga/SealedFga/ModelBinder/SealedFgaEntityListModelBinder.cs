using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SealedFga.Attributes;
using SealedFga.AuthModel;
using SealedFga.Fga;
using SealedFga.Generators.AuthModel;
using SealedFga.Util;

namespace SealedFga.ModelBinder;

/// <summary>
///     Model binder for binding FGA entity list parameters annotated with <see cref="FgaAuthorizeListAttribute" />.
/// </summary>
public class SealedFgaEntityListModelBinder(Type dbContextType)
    : SealedFgaModelBinder<FgaAuthorizeListAttribute>(dbContextType) {
    /// <summary>
    ///     Used to bind an FGA entities list parameter that has been annotated with the <see cref="FgaAuthorizeAttribute" />.
    ///     Retrieves all objects of the given type for which the user has the required <c>Relation</c>.
    ///     Loads the entities from the DB and injects them into the annotated parameter.
    /// </summary>
    /// <example>
    ///     <code>
    ///     public async Task&lt;IActionResult&gt; GetSecrets(
    ///         [FgaAuthorizeList(Relation = nameof(SecretEntityIdAttributes.can_view)]
    ///         List&lt;SecretEntity&gt; secrets
    ///     );
    /// </code>
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
        FgaAuthorizeListAttribute attr
    ) {
        var dbContext = GetDbContext(context);
        var entityType = context.ModelType.GetGenericArguments()[0]; // e.g. List<SecretEntity> -> SecretEntity

        // Get the ID type from ISealedFgaType<TId> interface
        var sealedFgaTypeInterface = entityType.GetInterfaces()
                                               .FirstOrDefault(i => i.IsGenericType &&
                                                                    i.GetGenericTypeDefinition() ==
                                                                    typeof(ISealedFgaType<>)
                                                );

        if (sealedFgaTypeInterface == null) {
            return; // Entity doesn't implement ISealedFgaType<TId> which should not be possible due to generic constraints
        }

        var idType = sealedFgaTypeInterface.GetGenericArguments()[0]; // e.g. SecretEntityId

        // Use reflection to get the OpenFGA type name from the static property
        var openFgaTypeNameProperty = idType.GetProperty(
            TypeNameIdGenerator.OpenFgaTypeNamePropertyName,
            BindingFlags.Public | BindingFlags.Static
        );
        var openFgaTypeName = (string?) openFgaTypeNameProperty?.GetValue(null);
        if (openFgaTypeName == null) {
            return;
        }

        // Get authorized object IDs using ListObjectsAsync with raw parameters
        var authorizedObjectStrings = await sealedFgaService.ListObjectsAsync(
            openFgaRawUserString,
            attr.Relation,
            openFgaTypeName
        );

        // Parse authorized IDs into strong ID types
        var authorizedIds = authorizedObjectStrings.Select(aos => IdUtil.ParseId(idType, aos)).ToList();

        // If no authorized entities, return empty list
        if (!authorizedIds.Any()) {
            var emptyList = Activator.CreateInstance(context.ModelType);
            context.Result = ModelBindingResult.Success(emptyList);
            return;
        }

        // Get the DbSet and filter by authorized IDs
        var dbSetProperty = dbContext.GetType().GetProperties()
                                     .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                                          p.PropertyType.GetGenericTypeDefinition() ==
                                                          typeof(DbSet<>) &&
                                                          p.PropertyType.GetGenericArguments()[0] == entityType
                                      );
        if (dbSetProperty == null) {
            return;
        }

        var dbSet = dbSetProperty.GetValue(dbContext);

        // Use LINQ to filter entities by authorized IDs
        var whereMethod = typeof(Queryable).GetMethods()
                                           .First(m => m.Name == nameof(Queryable.Where) &&
                                                       m.GetParameters().Length == 2
                                            )
                                           .MakeGenericMethod(entityType);

        // Create lambda: entity => authorizedIds.Contains(entity.Id)
        var parameter = Expression.Parameter(entityType, "entity");
        var idProperty = Expression.Property(parameter, nameof(ISealedFgaType<>.Id));
        var containsMethod = typeof(Enumerable).GetMethods()
                                               .First(m => m.Name == nameof(Enumerable.Contains) &&
                                                           m.GetParameters().Length == 2
                                                )
                                               .MakeGenericMethod(idType);
        var containsCall = Expression.Call(containsMethod, Expression.Constant(authorizedIds), idProperty);
        var lambda = Expression.Lambda(containsCall, parameter);

        var filteredQuery = whereMethod.Invoke(null, [dbSet, lambda]);

        // Convert to list
        var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))?.MakeGenericMethod(entityType);
        var entities = toListMethod?.Invoke(null, [filteredQuery]);

        context.Result = ModelBindingResult.Success(entities);
    }
}
