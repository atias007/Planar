using Planar.API.Common.Entities;
using System;
using System.Collections.Generic;

namespace Planar.Service.Model;

public partial class GlobalConfig
{
    public bool IsEncrypted => IsSecret && !string.IsNullOrWhiteSpace(SecretKey);

    public void Update(GlobalConfigModelRequest request)
    {
        Value = request.Value;
        SourceUrl = request.SourceUrl;
        LastUpdate = DateTime.Now;

        if (request.IsSecret.HasValue)
        {
            IsSecret = request.IsSecret.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            Type = string.IsNullOrWhiteSpace(request.Type) ? nameof(GlobalConfigTypes.String).ToLower() : request.Type;
        }
    }

    public static GlobalConfig FromGlobalConfigModelRequest(GlobalConfigModelRequest entity)
    {
        return new GlobalConfig
        {
            Key = entity.Key,
            Value = entity.Value,
            Type = string.IsNullOrWhiteSpace(entity.Type) ? nameof(GlobalConfigTypes.String).ToLower() : entity.Type,
            SourceUrl = entity.SourceUrl,
            IsSecret = entity.IsSecret.GetValueOrDefault(),
            LastUpdate = DateTime.Now
        };
    }

    public static GlobalConfigModel ToGlobalConfigModel(GlobalConfig entity)
    {
        return new GlobalConfigModel
        {
            Key = entity.Key,
            Value = entity.Value,
            Type = entity.Type,
            SourceUrl = entity.SourceUrl,
            IsSecret = entity.IsSecret,
            LastUpdate = entity.LastUpdate
        };
    }

    public static IEnumerable<GlobalConfigModel> ToGlobalConfigModel(IEnumerable<GlobalConfig> entities)
    {
        foreach (var entity in entities)
        {
            yield return ToGlobalConfigModel(entity);
        }
    }
}