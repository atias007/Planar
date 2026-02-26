using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Planar;

public class CleanupODataMediaTypesTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in path.Operations.Values)
            {
                if (operation.RequestBody?.Content == null) continue;

                var jsonSchema = operation.RequestBody.Content
                    .FirstOrDefault(c => c.Key == MediaTypeNames.Application.Json).Value
                    ?? operation.RequestBody.Content.First().Value;

                operation.RequestBody.Content.Clear();
                operation.RequestBody.Content[MediaTypeNames.Application.Json] = jsonSchema;
            }
        }

        return Task.CompletedTask;
    }
}