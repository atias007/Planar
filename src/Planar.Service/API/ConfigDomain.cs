using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetEscapades.Configuration.Yaml;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class ConfigDomain(IServiceProvider serviceProvider) : BaseLazyBL<ConfigDomain, ConfigData>(serviceProvider)
{
    public async Task Delete(string key)
    {
        key = key.SafeTrim() ?? string.Empty;
        var count = await DataLayer.RemoveGlobalConfig(key);
        if (count == 0)
        {
            throw new RestNotFoundException($"global config with key '{key}' not found");
        }

        await Flush();
    }

    public async Task Flush(CancellationToken stoppingToken = default)
    {
        await FlushInner(stoppingToken);
        if (AppSettings.Cluster.Clustering)
        {
            await ClusterUtil.ConfigFlush();
        }
    }

    public async Task FlushInner(CancellationToken stoppingToken = default)
    {
        var prms = await DataLayer.GetAllGlobalConfig(stoppingToken);
        var final = prms
            .Where(p => string.Equals(p.Type, GlobalConfigTypes.String.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key.Trim(), p => p.Value);

        var yamls = prms
            .Where(p =>
                string.Equals(p.Type, GlobalConfigTypes.Yml.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(p.Value));

        foreach (var y in yamls)
        {
            var ymlDic = GetYmlConfiguration(y);
            final = final.Merge(ymlDic);
        }

        var json = prms
            .Where(p => string.Equals(p.Type, GlobalConfigTypes.Json.ToString(), StringComparison.OrdinalIgnoreCase));

        foreach (var j in json)
        {
            var jsonDic = GetJsonConfiguration(j);
            final = final.Merge(jsonDic);
        }

        Global.SetGlobalConfig(final);
    }

    public async Task<GlobalConfig> Get(string key)
    {
        var data = await DataLayer.GetGlobalConfig(key) ?? throw new RestNotFoundException($"global config with key '{key}' not found");
        return data;
    }

    public async Task<IEnumerable<GlobalConfig>> GetAll()
    {
        var data = await DataLayer.GetAllGlobalConfig();
        return data;
    }

    public static IEnumerable<KeyValueItem> GetAllFlat()
    {
        var data = Global.GlobalConfig
            .OrderBy(kv => kv.Key)
            .Select(g => new KeyValueItem(g.Key.Trim(), g.Value));

        return data;
    }

    public async Task Put(GlobalConfig request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.IsGlobalConfigExists(request.Key);
        if (exists)
        {
            await DataLayer.UpdateGlobalConfig(request);
        }
        else
        {
            await DataLayer.AddGlobalConfig(request);
        }

        await Flush();
    }

    public async Task Add(GlobalConfig request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.IsGlobalConfigExists(request.Key);

        if (exists)
        {
            throw new RestConflictException($"key {request.Key} already exists");
        }

        await DataLayer.AddGlobalConfig(request);
        await Flush();
    }

    public async Task Update(GlobalConfig request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.IsGlobalConfigExists(request.Key);
        if (!exists)
        {
            throw new RestNotFoundException();
        }

        await DataLayer.UpdateGlobalConfig(request);
        await Flush();
    }

    private IDictionary<string, string?> GetYmlConfiguration(GlobalConfig config)
    {
        try
        {
            if (string.IsNullOrEmpty(config.Value)) { return new Dictionary<string, string?>(); }
            var dic = new YamlConfigurationFileParser().Parse(config.Value ?? string.Empty);
            return dic;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "invalid yml format at global config key '{Key}'", config.Key);
            return new Dictionary<string, string?>();
        }
    }

    private Dictionary<string, string?> GetJsonConfiguration(GlobalConfig config)
    {
        try
        {
            if (string.IsNullOrEmpty(config.Value)) { return []; }
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(config.Value.Trim());
            writer.Flush();
            stream.Position = 0;

            var items = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build()
                .AsEnumerable();

            var dic = new Dictionary<string, string?>(items);
            return dic;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "invalid json format at global config key '{Key}'", config.Key);
            return [];
        }
    }
}