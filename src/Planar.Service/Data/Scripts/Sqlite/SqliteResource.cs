using Planar.API.Common.Entities;
using System;
using System.IO;
using System.Reflection;

namespace Planar.Service.Data.Scripts.Sqlite;

internal static class SqliteResource
{
    private static readonly Assembly _assembly = typeof(SqliteResource).Assembly;

    public static string GetScript(string name, object? parameters = null)
    {
        string resourceName = $"Planar.Service.Data.Scripts.Sqlite.{name}.sql";

        using Stream? stream = _assembly.GetManifestResourceStream(resourceName);
        ArgumentNullException.ThrowIfNull(stream);
        // Use the stream to read the resource
        using StreamReader reader = new(stream);
        var resourceContent = reader.ReadToEnd() ?? string.Empty;
        if (parameters is IPagingRequest pagingRequest)
        {
            resourceContent = resourceContent.Replace("{{limit}}", pagingRequest.PageSize.ToString());
            resourceContent = resourceContent.Replace("{{offset}}", ((pagingRequest.PageNumber - 1) * pagingRequest.PageSize).ToString());
        }

        return resourceContent;
    }
}