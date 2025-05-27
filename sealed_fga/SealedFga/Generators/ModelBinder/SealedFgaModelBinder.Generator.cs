using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class SealedFgaModelBinderGenerator
{
    public static GeneratedFile Generate() => new(
        $"SealedFgaModelBinder.g.cs",
        $$"""

          public abstract class SealedFgaModelBinder<TAttr>(Type dbContextType) : IModelBinder where TAttr : Attribute
          {
              public async Task BindModelAsync(ModelBindingContext context)
              {
                  ArgumentNullException.ThrowIfNull(context);
          
                  // Retrieve fga entity parameter, e.g. SecretEntity
                  var param = context.ActionContext.ActionDescriptor
                      .Parameters
                      .OfType<ControllerParameterDescriptor>()
                      .FirstOrDefault(p => p.Name == context.FieldName);
                  var attr = (TAttr?)param?
                      .ParameterInfo
                      .GetCustomAttributes(typeof(TAttr), false)
                      .FirstOrDefault();
                  if (param is null || attr is null)
                  {
                      return;
                  }
          
                  await FgaBind(context, param, attr);
              }
          
              protected DbContext GetDbContext(ModelBindingContext context)
                  => (DbContext)context.HttpContext.RequestServices.GetRequiredService(dbContextType);
          
              protected abstract Task FgaBind(ModelBindingContext context, ControllerParameterDescriptor param, TAttr attr);
          }
          """,
        new HashSet<string>([
            "System",
            "System.Linq",
            "System.Threading.Tasks",
            "Microsoft.AspNetCore.Mvc.Controllers",
            "Microsoft.AspNetCore.Mvc.ModelBinding",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions.DependencyInjection"
        ]),
        Settings.PackageNamespace + ".ModelBinder"
    );
}