using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SealedFga.Sample.Database;
using SealedFga.Sample.Secret;

namespace SealedFga.Sample.FgaAuthorization;

public class FgaEntityModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var param = context.ActionContext.ActionDescriptor
            .Parameters
            .OfType<ControllerParameterDescriptor>()
            .FirstOrDefault(p => p.Name == context.FieldName);

        var fgaAuthAttribute = (FgaAuthorizeAttribute?)param?
            .ParameterInfo
            .GetCustomAttributes(typeof(FgaAuthorizeAttribute), false)
            .FirstOrDefault();
        if (fgaAuthAttribute == null)
        {
            return;
        }

        // Get the value of the parameter specified in ParameterName
        var parameterVal = (string?)context.ActionContext.RouteData.Values[fgaAuthAttribute.ParameterName];
        if (parameterVal == null)
        {
            return;
        }

        var idVal = SecretEntityId.Parse(parameterVal);

        // Get the entity from database
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<SealedFgaSampleContext>();
        var entity = await dbContext.FindAsync(context.ModelType, idVal);
        if (entity == null)
        {
            return;
        }

        context.Result = ModelBindingResult.Success(entity);
    }
}