using Planar.API.Common.Entities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Planar.Service.Model;

public partial class GlobalConfig
{
    public bool IsEncrypted => IsSecret && !string.IsNullOrWhiteSpace(SecretKey);

    public static void UpdateGlobalConfig(GlobalConfig entity, GlobalConfigModelRequest request)
    {
        entity.Value = request.Value;
        entity.Type = string.IsNullOrWhiteSpace(request.Type) ? nameof(GlobalConfigTypes.String).ToLower() : request.Type;
        entity.SourceUrl = request.SourceUrl;
        entity.IsSecret = request.IsSecret.GetValueOrDefault();
        entity.LastUpdate = DateTime.Now;
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