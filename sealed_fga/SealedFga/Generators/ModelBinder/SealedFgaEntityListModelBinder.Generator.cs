using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class SealedFgaEntityListModelBinderGenerator
{
    public static GeneratedFile Generate() => new(
        $"SealedFgaEntityListModelBinder.g.cs",
        $$"""

          public class SealedFgaEntityListModelBinder(Type dbContextType)
              : SealedFgaModelBinder<FgaAuthorizeListAttribute>(dbContextType)
          {
              /// <summary>
              ///     Used to bind an FGA entities list parameter that has been annotated with the <see cref="FgaAuthorizeAttribute"/>.
              ///     Retrieves all objects of the given type for which the user has the required <c>Relation</c>.
              ///     Loads the entities from the DB and injects them into the annotated parameter.
              /// </summary>
              /// <example>
              ///     <code>
              ///     public async Task&lt;IActionResult&gt; GetSecrets(
              ///         [FgaAuthorizeList(Relation = nameof(SecretEntityIdAttributes.can_view)]
              ///         List<SecretEntity> secrets
              ///     ); 
              ///     </code>
              /// </example>
              /// <param name="context">The model binding context.</param>
              /// <param name="param">The annotated parameter.</param>
              /// <param name="attr">The parameter's annotation.</param>
              protected override async Task FgaBind(ModelBindingContext context, ControllerParameterDescriptor param,
                  FgaAuthorizeListAttribute attr)
              {
                  var dbContext = GetDbContext(context);
                  var entityType = context.ModelType.GetGenericArguments()[0]; // e.g. List<SecretEntity> -> SecretEntity
          
                  // Get the DbSet property using reflection, e.g. DbSet<SecretEntity>
                  var dbSetProperty = dbContext.GetType().GetProperties()
                      .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                           p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                           p.PropertyType.GetGenericArguments()[0] == entityType);
                  if (dbSetProperty == null)
                  {
                      return;
                  }
          
                  // Get all entities from DbSet via .ToList()
                  var dbSet = dbSetProperty.GetValue(dbContext);
                  var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))?.MakeGenericMethod(entityType);
                  var entities = toListMethod?.Invoke(null, [dbSet]);
          
                  context.Result = ModelBindingResult.Success(entities);
              }
          }
          """,
        new HashSet<string>([
            "System",
            "System.Linq",
            "System.Threading.Tasks",
            "Microsoft.AspNetCore.Mvc.Controllers",
            "Microsoft.AspNetCore.Mvc.ModelBinding",
            "Microsoft.EntityFrameworkCore",
            "SealedFga",
            "SealedFga.Attributes"
        ]),
        Settings.ModelBindingNamespace
    );
}