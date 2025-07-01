using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SealedFga.ModelBinder;

/// <summary>
///     Abstract base class for FGA model binders.
/// </summary>
/// <typeparam name="TAttr">The attribute's type to annotate model binding.</typeparam>
/// <param name="dbContextType">The type of the database context.</param>
public abstract class SealedFgaModelBinder<TAttr>(Type dbContextType) : IModelBinder where TAttr : Attribute {
    /// <inheritdoc />
    public async Task BindModelAsync(ModelBindingContext context) {
        // Retrieve fga entity parameter, e.g. SecretEntity
        var param = context.ActionContext.ActionDescriptor
                           .Parameters
                           .OfType<ControllerParameterDescriptor>()
                           .FirstOrDefault(p => p.Name == context.FieldName);
        var attr = (TAttr?) param?
                           .ParameterInfo
                           .GetCustomAttributes(typeof(TAttr), false)
                           .FirstOrDefault();
        if (param is null || attr is null) {
            return;
        }

        await FgaBind(context, param, attr);
    }

    /// <summary>
    ///     Gets the database context from the model binding context.
    /// </summary>
    /// <param name="context">The model binding context.</param>
    /// <returns>The database context.</returns>
    protected DbContext GetDbContext(ModelBindingContext context)
        => (DbContext) context.HttpContext.RequestServices.GetRequiredService(dbContextType);

    /// <summary>
    ///     Performs the FGA-specific binding logic.
    /// </summary>
    /// <param name="context">The model binding context.</param>
    /// <param name="param">The controller parameter descriptor.</param>
    /// <param name="attr">The attribute instance.</param>
    protected abstract Task FgaBind(ModelBindingContext context, ControllerParameterDescriptor param, TAttr attr);
}
