using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserIdentityApi.Infrastructure.Filters;

/// <summary>
/// Swagger için varsayılan değerleri ayarlayan operasyon filtresi
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    /// <summary>
    /// Operasyon parametrelerini ve yanıtlarını yapılandırır
    /// </summary>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // RESTful endpoint açıklamaları
        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = context.ApiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            if (description == null)
            {
                continue;
            }

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                var json = JsonSerializer.Serialize(description.DefaultValue, description.ModelMetadata.ModelType);
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }

            parameter.Required |= description.IsRequired;
        }
    }
} 