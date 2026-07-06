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
    public async Task<IEnumerable<KeyValueItem>> GetAllFlat(CancellationToken stoppingToken = default)
    {
        var final = await LoadConfigFlat(decrypt: false, stoppingToken);
        return final.Select(kv => new KeyValueItem { Key = kv.Key, Value = kv.Value });
    }

    public async Task Add(GlobalConfigModelAddRequest request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.IsGlobalConfigExists(request.Key);

        if (exists)
        {
            throw new RestConflictException($"key {request.Key} already exists");
        }

        await SetValueSourceUrlContent(request);

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
        var final = await LoadConfigFlat(decrypt: true, stoppingToken);
        Global.SetGlobalConfig(final);
    }

    private async Task<Dictionary<string, string?>> LoadConfigFlat(bool decrypt, CancellationToken stoppingToken = default)
    {
        var prms = await DataLayer.GetAllGlobalConfig(stoppingToken);
        var final = new Dictionary<string, string?>();
        foreach (var p in prms)
        {
            // string
            if (string.Equals(p.Type, GlobalConfigTypes.String.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                final.Put(p.Key.Trim(), p.Value);
            } // yml
            else if (
                string.Equals(p.Type, GlobalConfigTypes.Yml.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.Value))
            {
                var ymlDic = GetYmlConfiguration(p, decrypt);
                final = final.Merge(ymlDic);
            }
            else if (
                string.Equals(p.Type, GlobalConfigTypes.Json.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.Value))
            {
                var jsonDic = GetJsonConfiguration(p, decrypt);
                final = final.Merge(jsonDic);
            }
        }

        return final;
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
                await SetValueSourceUrlContent(request);
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

    public async Task Update(GlobalConfigModelUpdateRequest request)
    {
        request.Key = request.Key.Trim();
        var exists = await DataLayer.GetGlobalConfig(request.Key) ?? throw new RestNotFoundException();
        if (!string.IsNullOrWhiteSpace(exists.SourceUrl))
        {
            if (!string.IsNullOrWhiteSpace(request.Value)) { throw new RestValidationException(nameof(request.Value), $"config key '{request.Key}' has source url '{exists.SourceUrl}' and cannot be updated with value"); }
            if (string.IsNullOrWhiteSpace(request.SourceUrl)) { throw new RestValidationException(nameof(request.SourceUrl), $"config key '{request.Key}' has source url '{exists.SourceUrl} and your update request must have source url value"); }
            var content = await SafeGetSourceUrlContent(request.SourceUrl);
            exists.SourceUrl = request.SourceUrl;
            exists.Value = content;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.SourceUrl)) { throw new RestValidationException(nameof(request.SourceUrl), $"config key '{request.Key}' has no source url and cannot be updated with new source url"); }
            exists.Value = request.Value;
            exists.SourceUrl = null;
        }

        EncryptConfigValueIfNeeded(exists);

        await DataLayer.UpdateGlobalConfig(exists);
        AuditSecuritySafe($"config key '{request.Key}' was updated");
        _ = Flush();
    }

    private static string? EncryptConfigValueIfNeeded(GlobalConfigModelAddRequest request)
    {
        if (request.IsSecret != true) { return null; }
        if (string.IsNullOrWhiteSpace(request.Value)) { return null; }
        var key = Aes256Cipher.GenerateKey();
        var aes = new Aes256Cipher(key);
        request.Value = aes.Encrypt(request.Value);
        return key;
    }

    private static void EncryptConfigValueIfNeeded(GlobalConfig globalConfig)
    {
        if (string.IsNullOrWhiteSpace(globalConfig.Value)) { return; }
        if (!globalConfig.IsSecret)
        {
            globalConfig.SecretKey = null;
            return;
        }

        var key = Aes256Cipher.GenerateKey();
        var aes = new Aes256Cipher(key);
        globalConfig.Value = aes.Encrypt(globalConfig.Value);
        globalConfig.SecretKey = key;
    }

    private static async Task<string> SafeGetSourceUrlContent(string sourceUrl)
    {
        try
        {
            return await GetSourceUrlContent(sourceUrl);
        }
        catch (Exception ex)
        {
            throw new RestValidationException("source url", $"unable to get content from source url '{sourceUrl}'. message: {ex.Message}");
        }
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

    private static async Task SetValueSourceUrlContent(GlobalConfigModelAddRequest request)
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

    private string? GetGlobalConfigValue(GlobalConfig config, bool decrypt)
    {
        if (!config.IsEncrypted) { return config.Value; }
        if (string.IsNullOrWhiteSpace(config.Value)) { return config.Value; }
        if (!decrypt) { return config.Value; }

        try
        {
            var aes = new Aes256Cipher(config.SecretKey ?? string.Empty);
            var value = aes.Decrypt(config.Value);
            return value;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "unable to decrypt global config key '{Key}'", config.Key);
            return config.Value;
        }
    }

    private Dictionary<string, string?> GetJsonConfiguration(GlobalConfig config, bool decrypt)
    {
        try
        {
            var value = GetGlobalConfigValue(config, decrypt);
            if (string.IsNullOrWhiteSpace(value)) { return []; }
            if (config.IsEncrypted && !decrypt) { return new Dictionary<string, string?> { [config.Key] = config.Value }; }

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

    private Dictionary<string, string?> GetYmlConfiguration(GlobalConfig config, bool decrypt)
    {
        try
        {
            var value = GetGlobalConfigValue(config, decrypt);
            if (string.IsNullOrWhiteSpace(value)) { return []; }
            if (config.IsEncrypted && !decrypt) { return new Dictionary<string, string?> { [config.Key] = config.Value }; }

            var dic = new YamlConfigurationFileParser().Parse(value);
            return dic.ToDictionary();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "invalid yml format at global config key '{Key}'", config.Key);
            return [];
        }
    }
}