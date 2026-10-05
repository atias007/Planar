using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetEscapades.Configuration.Yaml;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
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
    private const string c_source_url = "source url";
    private const string kind = "global config";

    public async Task Add(GlobalConfigModelAddRequest request)
    {
        TrimConfigKey(request);
        var exists = await DataLayer.IsGlobalConfigExists(request.Key);

        if (exists)
        {
            throw new RestConflictException($"key {request.Key} already exists");
        }

        await AddInner(request, withDelete: false);
        _ = Flush();
    }

    public async Task<ApplyResponse> Apply(HttpContext httpContext)
    {
        var yamls = await GetApplyYamls(httpContext, kind);
        var result = await Apply(yamls, httpContext.RequestAborted);
        return result;
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

    public async Task ConvertToSecret(string key)
    {
        key = key.SafeTrim() ?? string.Empty;
        var exists = await DataLayer.GetGlobalConfigForUpdate(key) ?? throw new RestNotFoundException();
        if (exists.IsSecret)
        {
            throw new RestValidationException(nameof(key), $"global config '{key}' is already secret");
        }

        exists.IsSecret = true;

        // encrypt the value if needed and get the secret key
        EncryptConfigValueIfNeeded(exists);

        await DataLayer.SaveChangesAsync();
        AuditSecuritySafe($"config key '{key}' was converted to secret");
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
                Type = config.Type,
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
                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation("config key '{Key}' was reloaded", updatedConfig.Key);
                }
            }
        }

        await Flush(cancellationToken);
        if (message != null)
        {
            throw new RestValidationException(c_source_url, message);
        }
    }

    public async Task<GlobalConfigModel> Get(string key)
    {
        key = key.SafeTrim() ?? string.Empty;
        var data = await DataLayer.GetGlobalConfig(key) ?? throw new RestNotFoundException($"global config with key '{key}' not found");
        var result = GlobalConfig.ToGlobalConfigModel(data);
        return result;
    }

    public async Task<PagingResponse<GlobalConfigModel>> GetAll(PagingRequest request)
    {
        var data = await DataLayer.GetAllGlobalConfigWithPaging(request);
        var items = GlobalConfig.ToGlobalConfigModel(data.Data ?? []).ToList();
        return new PagingResponse<GlobalConfigModel>(request, items, data.TotalRows);
    }

    public async Task<PagingResponse<KeyValueItem>> GetAllFlat(PagingRequest request, CancellationToken stoppingToken = default)
    {
        var final = await LoadConfigFlat(decrypt: false, stoppingToken);
        var items = final.Select(kv => new KeyValueItem { Key = kv.Key, Value = kv.Value })
            .SetPaging(request)
            .ToList();
        return new PagingResponse<KeyValueItem>(request, items, final.Count);
    }

    public async Task<IEnumerable<string>> GetAllKeys()
    {
        var data = await DataLayer.GetAllGlobalConfigKeys();
        return data;
    }

    public async Task<int> Update(GlobalConfigModelUpdateRequest request)
    {
        TrimConfigKey(request);
        var exists = await DataLayer.GetGlobalConfigForUpdate(request.Key) ?? throw new RestNotFoundException();

        if (!string.IsNullOrWhiteSpace(exists.SourceUrl))
        {
            if (!string.IsNullOrWhiteSpace(request.Value)) { throw new RestValidationException(nameof(request.Value), $"config key '{request.Key}' has source url '{exists.SourceUrl}' and cannot be updated with value"); }
            if (string.IsNullOrWhiteSpace(request.SourceUrl)) { throw new RestValidationException(nameof(request.SourceUrl), $"config key '{request.Key}' has source url '{exists.SourceUrl} and your update request must have source url value"); }
            if (exists.SourceUrl == request.SourceUrl) { return 0; }
            request.Value = await SafeGetSourceUrlContent(request.SourceUrl);
            exists.SourceUrl = request.SourceUrl;
            exists.Value = request.Value;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.SourceUrl)) { throw new RestValidationException(nameof(request.SourceUrl), $"config key '{request.Key}' has no source url and cannot be updated with new source url"); }
            exists.Value = request.Value;
            exists.SourceUrl = null;
        }

        // validate that the content matches the specified type (yml, json, string)
        ValidateContentMatchTheType(request, exists.Type);

        // encrypt the value if needed and get the secret key
        EncryptConfigValueIfNeeded(exists);

        var count = await DataLayer.SaveChangesAsync();
        if (count > 0)
        {
            AuditSecuritySafe($"config key '{request.Key}' was updated");
            _ = Flush();
        }

        return count;
    }

    internal async Task<ApplyResponse> Apply(IEnumerable<KeyValuePair<string, string>> yamls, CancellationToken cancellationToken)
    {
        // Convert to list of GlobalConfigApplyRequest
        var requests = await GetApplyEntities<GlobalConfigApplyRequest>(yamls, kind, cancellationToken);

        // Validation
        ValidateDuplicateApplyRequests(requests, r => r.Key, kind, "key");

        // Apply changes
        var response = await ApplyChanges(requests);

        // Save changes
        await DataLayer.SaveChangesAsync();

        // Clear cache
        await Flush(cancellationToken);

        return response;
    }

    private static string? EncryptConfigValueIfNeeded(GlobalConfigModelAddRequest request)
    {
        if (request.IsSecret != true) { return null; }
        return EncryptConfigValueIfNeeded(request, request.IsSecret);
    }

    private static string? EncryptConfigValueIfNeeded(GlobalConfigModelUpdateRequest request, bool? isSecret)
    {
        if (isSecret != true) { return null; }
        if (string.IsNullOrWhiteSpace(request.Value)) { return null; }
        var key = Aes256Cipher.GenerateKey();
        var aes = new Aes256Cipher(key);
        request.Value = aes.Encrypt(request.Value);
        return key;
    }

    private static void EncryptConfigValueIfNeeded(GlobalConfig config)
    {
        if (!config.IsSecret) { return; }
        if (string.IsNullOrWhiteSpace(config.Value)) { return; }
        var key = Aes256Cipher.GenerateKey();
        var aes = new Aes256Cipher(key);
        config.Value = aes.Encrypt(config.Value);
        config.SecretKey = key;
    }

    private static async Task<string> GetSourceUrlContent(string sourceUrl)
    {
        string content;
        var uri = new Uri(sourceUrl);
        if (uri.IsFile && uri.IsAbsoluteUri)
        {
            content = await File.ReadAllTextAsync(uri.LocalPath);
        }
        else if (uri.IsFile && !uri.IsAbsoluteUri)
        {
            var path = Path.Combine(FolderConsts.BasePath, uri.LocalPath);
            content = await File.ReadAllTextAsync(path);
        }
        else
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(sourceUrl);
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
        }

        const int maxLength = 4_000;
        if (content.Length > maxLength)
        {
            throw new RestValidationException(c_source_url, $"source url '{sourceUrl}' content has more then {maxLength:N0} characters");
        }

        return content;
    }

    private static async Task<string> SafeGetSourceUrlContent(string sourceUrl)
    {
        try
        {
            return await GetSourceUrlContent(sourceUrl);
        }
        catch (Exception ex)
        {
            throw new RestValidationException(c_source_url, $"unable to get content from source url '{sourceUrl}'. message: {ex.Message}");
        }
    }

    private static void SetDefaultConfigType(GlobalConfigModelAddRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Type)) { request.Type = nameof(GlobalConfigTypes.String).ToLower(); }
    }

    private static async Task SetValueSourceUrlContent(GlobalConfigModelAddRequest request)
    {
        if (string.IsNullOrEmpty(request.SourceUrl)) { return; }
        try
        {
            var content = await GetSourceUrlContent(request.SourceUrl);
            request.Value = content;
        }
        catch (Exception ex)
        {
            throw new RestValidationException(c_source_url, $"unable to get content from source url '{request.SourceUrl}'. message: {ex.Message}");
        }
    }

    private static void TrimConfigKey(GlobalConfigModelUpdateRequest request)
    {
        request.Key = request.Key.Trim();
    }

    private static void ValidateContentMatchTheType(GlobalConfigModelUpdateRequest request, string? type)
    {
        if (string.IsNullOrWhiteSpace(request.Value)) { return; }

        var isYaml = string.Equals(type, GlobalConfigTypes.Yml.ToString(), StringComparison.OrdinalIgnoreCase);
        var isJson = string.Equals(type, GlobalConfigTypes.Json.ToString(), StringComparison.OrdinalIgnoreCase);
        if (!isYaml && !isJson) { return; }

        if (isYaml && !ValidationUtil.IsYmlValid(request.Value))
        {
            throw new RestValidationException(nameof(request.Value), $"value has invalid yml format");
        }
        else if (isJson && !ValidationUtil.IsJsonValid(request.Value))
        {
            throw new RestValidationException(nameof(request.Value), $"value has invalid json format");
        }
    }

    private async Task AddInner(GlobalConfigModelAddRequest request, bool withDelete)
    {
        // Set default type to string if not provided
        SetDefaultConfigType(request);

        // read content from source url if provided and set it to request.Value
        await SetValueSourceUrlContent(request);

        // validate that the content matches the specified type (yml, json, string)
        ValidateContentMatchTheType(request, request.Type);

        // encrypt the value if needed and get the secret key
        var secretKey = EncryptConfigValueIfNeeded(request);

        // map the request to the GlobalConfig entity and set the secret key
        var globalConfig = GlobalConfig.FromGlobalConfigModelAddRequest(request);
        globalConfig.SecretKey = secretKey;

        // create data layer
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dataLayer = scope.ServiceProvider.GetRequiredService<IConfigData>();

        if (withDelete)
        {
            await dataLayer.AddGlobalConfigWithDelete(globalConfig);
            AuditSecuritySafe($"config key '{request.Key}' was updated");
        }
        else
        {
            await dataLayer.AddGlobalConfig(globalConfig);
            AuditSecuritySafe($"config key '{request.Key}' was added");
        }
    }

    private async Task<ApplyResponse> ApplyChanges(IReadOnlyCollection<GlobalConfigApplyRequest> requests)
    {
        var response = new ApplyResponse();
        if (requests.Count == 0) { return response; }
        if (requests.Count == 1)
        {
            var request = requests.First();
            var result = await ApplyInner(request);
            response.AddItem(result);
            return response;
        }

        foreach (var request in requests)
        {
            try
            {
                var result = await ApplyInner(request);
                response.AddItem(result);
            }
            catch (Exception ex)
            {
                response.AddItem(new ApplyResponseItem(request.Key, ApplyAction.Error, $"fail to handle global config '{request.Key}'. {ex.Message}", Manifest.GlobalConfig, request.Source));
            }
        }

        return response;
    }

    private async Task<ApplyResponseItem> ApplyInner(GlobalConfigApplyRequest request)
    {
        TrimConfigKey(request);
        SetDefaultConfigType(request);
        var exists = await DataLayer.GetGlobalConfigForUpdate(request.Key);
        if (exists == null)
        {
            await AddInner(request, withDelete: false);
            var message = $"global config '{request.Key}' was added";
            return new ApplyResponseItem(request.Key, ApplyAction.Add, message, Manifest.GlobalConfig, request.Source);
        }
        else
        {
            var count = 0;
            if (await NeedToUpdate(request, exists))
            {
                if (exists.IsSecret && !request.IsSecret.GetValueOrDefault())
                {
                    throw new RestValidationException(nameof(request.IsSecret), $"global config '{request.Key}' is secret and cannot be updated to non-secret");
                }

                await AddInner(request, withDelete: true);
                count = 1;
            }

            var message = count > 0 ? $"global config '{request.Key}' was updated" : $"global config '{request.Key}' was not changed";
            var action = count > 0 ? ApplyAction.Update : ApplyAction.Unchanged;
            return new ApplyResponseItem(request.Key, action, message, Manifest.GlobalConfig, request.Source);
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

    private async Task<Dictionary<string, string?>> LoadConfigFlat(bool decrypt, CancellationToken stoppingToken = default)
    {
        var parameters = await DataLayer.GetAllGlobalConfig(stoppingToken);
        var final = new Dictionary<string, string?>();
        foreach (var p in parameters)
        {
            // string
            if (string.Equals(p.Type, GlobalConfigTypes.String.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var value = GetGlobalConfigValue(p, decrypt);
                final.Put(p.Key.Trim(), value);
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

    private async Task<bool> NeedToUpdate(GlobalConfigModelAddRequest request, GlobalConfig exists)
    {
        if (exists.SourceUrl != request.SourceUrl) { return true; }
        if (exists.IsSecret != request.IsSecret) { return true; }
        if (exists.Type != request.Type) { return true; }
        if (exists.IsSecret)
        {
            var existsValue = GetGlobalConfigValue(exists, decrypt: true);
            if (existsValue != request.Value) { return true; }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.SourceUrl) && exists.Value != request.Value) { return true; }
        }

        return false;
    }
}