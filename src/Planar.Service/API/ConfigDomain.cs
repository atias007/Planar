using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetEscapades.Configuration.Yaml;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class ConfigDomain(IServiceProvider serviceProvider) : BaseLazyBL<ConfigDomain, IConfigData>(serviceProvider)
{
    public static IEnumerable<KeyValueItem> GetAllFlat()
    {
        var data = Global.GlobalConfig
            .OrderBy(kv => kv.Key)
            .Select(g => new KeyValueItem(g.Key.Trim(), g.Value));

        return data;
    }

    public async Task Add(GlobalConfigModelAddRequest request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.IsGlobalConfigExists(request.Key);

        if (exists)
        {
            throw new RestConflictException($"key {request.Key} already exists");
        }

        await SetSourceUrlContent(request);

        var secretKey = EncryptConfigValueIfNeeded(request);
        var globalConfig = GlobalConfig.FromGlobalConfigModelAddRequest(request);
        globalConfig.SecretKey = secretKey;
        await DataLayer.AddGlobalConfig(globalConfig);
        AuditSecuritySafe($"config key '{request.Key}' was added");
        _ = Flush();
    }

    public async Task Delete(string key)
    {
        key = key.SafeTrim() ?? string.Empty;
        var count = await DataLayer.RemoveGlobalConfig(key);
        if (count == 0)
        {
            throw new RestNotFoundException($"global config with key '{key}' not found");
        }

        AuditSecuritySafe($"config key '{key}' was deleted");

        _ = Flush();
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

        // string
        var final = prms
            .Where(p => string.Equals(p.Type, GlobalConfigTypes.String.ToString(), StringComparison.OrdinalIgnoreCase))
            .Select(p => new
            {
                p.Key,
                Value = GetGlobalConfigValue(p)
            })
            .ToDictionary(p => p.Key.Trim(), p => p.Value);

        // yml
        var yamls = prms
            .Where(p =>
                string.Equals(p.Type, GlobalConfigTypes.Yml.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.Value));

        foreach (var y in yamls)
        {
            var ymlDic = GetYmlConfiguration(y);
            final = final.Merge(ymlDic);
        }

        // json
        var json = prms
            .Where(p => string.Equals(p.Type, GlobalConfigTypes.Json.ToString(), StringComparison.OrdinalIgnoreCase));

        foreach (var j in json)
        {
            var jsonDic = GetJsonConfiguration(j);
            final = final.Merge(jsonDic);
        }

        Global.SetGlobalConfig(final);
    }

    public async Task FlushWithReloadExternalSourceUrl(CancellationToken cancellationToken = default)
    {
        var allConfigs = await DataLayer.GetExternalSourceGlobalConfig(cancellationToken);
        string? message = null;
        foreach (var config in allConfigs)
        {
            var request = new GlobalConfigModelAddRequest
            {
                Key = config.Key,
                SourceUrl = config.SourceUrl,
            };

            try
            {
                await SetSourceUrlContent(request);
            }
            catch (Exception ex)
            {
                message ??= $"unable to reload source url for config key '{config.Key}' with url '{config.SourceUrl}'. message: {ex.Message}";
                Logger.LogError(ex, "unable to reload source url for config key '{Key}' with url '{SourceUrl}'", config.Key, config.SourceUrl);
            }

            if (config.Value != request.Value)
            {
                var updatedConfig = GlobalConfig.FromGlobalConfigModelAddRequest(request);
                await DataLayer.UpdateGlobalConfig(updatedConfig);
                Logger.LogInformation("config key '{Key}' was reloaded", updatedConfig.Key);
            }
        }

        await Flush(cancellationToken);
        if (message != null)
        {
            throw new RestValidationException("source url", message);
        }
    }

    public async Task<GlobalConfigModel> Get(string key)
    {
        key = key.SafeTrim() ?? string.Empty;
        var data = await DataLayer.GetGlobalConfig(key) ?? throw new RestNotFoundException($"global config with key '{key}' not found");
        var result = GlobalConfig.ToGlobalConfigModel(data);
        return result;
    }

    public async Task<IEnumerable<GlobalConfigModel>> GetAll()
    {
        var data = await DataLayer.GetAllGlobalConfig();
        var result = GlobalConfig.ToGlobalConfigModel(data);
        return result;
    }

    public async Task Update(GlobalConfigModelAddRequest request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.GetGlobalConfig(request.Key) ?? throw new RestNotFoundException();

        await SetSourceUrlContent(request);

        request.Value ??= exists.Value;
        request.SourceUrl ??= exists.SourceUrl;

        var secretKey = EncryptConfigValueIfNeeded(request);

        var globalConfig = GlobalConfig.FromGlobalConfigModelAddRequest(request);
        globalConfig.SecretKey = secretKey;
        await DataLayer.UpdateGlobalConfig(globalConfig);
        AuditSecuritySafe($"config key '{request.Key}' was updated");
        _ = Flush();
    }

    private static string? EncryptConfigValueIfNeeded(GlobalConfigModelAddRequest request)
    {
        if (!request.IsSecret) { return null; }
        if (string.IsNullOrWhiteSpace(request.Value)) { return null; }
        var key = Aes256Cipher.GenerateKey();
        var aes = new Aes256Cipher(key);
        request.Value = aes.Encrypt(request.Value);
        return key;
    }

    private static async Task<string> GetSourceUrlContent(string sourceUrl)
    {
        var uri = new Uri(sourceUrl);
        if (uri.IsFile && uri.IsAbsoluteUri)
        {
            return await File.ReadAllTextAsync(uri.LocalPath);
        }
        else if (uri.IsFile && !uri.IsAbsoluteUri)
        {
            var path = Path.Combine(FolderConsts.BasePath, uri.LocalPath);
            return await File.ReadAllTextAsync(path);
        }
        else
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(sourceUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
    }

    private static async Task SetSourceUrlContent(GlobalConfigModelAddRequest request)
    {
        if (string.IsNullOrEmpty(request.SourceUrl)) { return; }
        try
        {
            var content = await GetSourceUrlContent(request.SourceUrl);
            if (string.IsNullOrWhiteSpace(request.Type))
            {
                if (ValidationUtil.IsJsonValid(content)) { request.Type = nameof(GlobalConfigTypes.Json).ToLower(); }
                else if (ValidationUtil.IsYmlValid(content)) { request.Type = nameof(GlobalConfigTypes.Yml).ToLower(); }
                else { throw new InvalidDataException("source url content type could not be determined. content should be json or yml format"); }

                request.Value = content;
                return;
            }

            if (
                request.Type.Equals(nameof(GlobalConfigTypes.Json), StringComparison.OrdinalIgnoreCase)
                && !ValidationUtil.IsJsonValid(content))
            {
                throw new InvalidDataException("source url content is not valid json format");
            }

            if (
                request.Type.Equals(nameof(GlobalConfigTypes.Yml), StringComparison.OrdinalIgnoreCase)
                && !ValidationUtil.IsYmlValid(content))
            {
                throw new InvalidDataException("source url content is not valid yml format");
            }

            request.Value = content;
        }
        catch (Exception ex)
        {
            throw new RestValidationException("source url", $"unable to get content from source url '{request.SourceUrl}'. message: {ex.Message}");
        }
    }

    private string? GetGlobalConfigValue(GlobalConfig config)
    {
        if (!config.IsSecret) { return config.Value; }
        if (string.IsNullOrWhiteSpace(config.SecretKey)) { return config.Value; }
        if (string.IsNullOrWhiteSpace(config.Value)) { return config.Value; }

        try
        {
            var aes = new Aes256Cipher(config.SecretKey);
            var value = aes.Decrypt(config.Value);
            return value;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "unable to decrypt global config key '{Key}'", config.Key);
            return config.Value;
        }
    }

    private Dictionary<string, string?> GetJsonConfiguration(GlobalConfig config)
    {
        try
        {
            var value = GetGlobalConfigValue(config);
            if (string.IsNullOrWhiteSpace(value)) { return []; }

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(value.Trim());
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

    private IDictionary<string, string?> GetYmlConfiguration(GlobalConfig config)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(config.Value)) { return new Dictionary<string, string?>(); }
            var value = GetGlobalConfigValue(config);
            if (string.IsNullOrWhiteSpace(value)) { return new Dictionary<string, string?>(); }
            var dic = new YamlConfigurationFileParser().Parse(value);
            return dic;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "invalid yml format at global config key '{Key}'", config.Key);
            return new Dictionary<string, string?>();
        }
    }
}