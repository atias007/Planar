﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.DataProtect;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("service", "Actions to operate service, check alive, list calendars and more")]
public class ServiceCliActions : BaseCliAction<ServiceCliActions>
{
    [Action("halt")]
    public static async Task<CliActionResponse> StopScheduler(CancellationToken cancellationToken = default)
    {
        if (!ConfirmAction("halt (stop) planar service")) { return CliActionResponse.Empty; }

        var restRequest = new RestRequest("service/halt", Method.Post);
        return await Execute(restRequest, cancellationToken);
    }

    [Action("start")]
    public static async Task<CliActionResponse> StartScheduler(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service/start", Method.Post);
        return await Execute(restRequest, cancellationToken);
    }

    [Action("info")]
    public static async Task<CliActionResponse> GetInfo(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service", Method.Get);
        var result = await RestProxy.Invoke<AppSettingsInfo>(restRequest, cancellationToken);

        var data =
            result.Data == null ?
            [] :
            new List<CliDumpObject>
            {
                     new (result.Data.Authentication){ Title=nameof(result.Data.Authentication) },
                     new (result.Data.Cluster){ Title=nameof(result.Data.Cluster) },
                     new (result.Data.Database){ Title=nameof(result.Data.Database) },
                     new (result.Data.General){ Title=nameof(result.Data.General) },
                     new (result.Data.Retention){ Title=nameof(result.Data.Retention) },
                     new (result.Data.Monitor){ Title=nameof(result.Data.Monitor) },
                     new (result.Data.Protection) { Title = nameof(result.Data.Protection)},
                        new (result.Data.Smtp){ Title = nameof(result.Data.Smtp) },
                     new (result.Data.Hooks) { Title = nameof(result.Data.Hooks)},
            };

        return new CliActionResponse(result, data);
    }

    [Action("version")]
    public static async Task<CliActionResponse> GetVersion(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service/version", Method.Get);
        var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);

        if (result.IsSuccessful && result.Data != null)
        {
            var versionData = new CliVersionData
            {
                ServiceVersion = result.Data,
                CliVersion = Program.Version
            };

            var table = CliTableExtensions.GetTable(versionData);
            return new CliActionResponse(result, table);
        }

        return new CliActionResponse(result);
    }

    [Action("hc")]
    [Action("health-check")]
    public static async Task<CliActionResponse> HealthCheck(CancellationToken cancellationToken = default)
    {
        const string seperator = "-------------------";
        var restRequest = new RestRequest("service/health-check", Method.Get);
        var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
        if (result.IsSuccessful)
        {
            AnsiConsole.MarkupLine($"[green]{seperator}\r\nplanar is healthy\r\n{seperator}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{seperator}\r\nplanar is unhealthy\r\n{seperator}[/]");
        }

        return new CliActionResponse(result, result.Data);
    }

    [Action("env")]
    public static async Task<CliActionResponse> GetEnvironment(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service", Method.Get);
        var result = await RestProxy.Invoke<AppSettingsInfo>(restRequest, cancellationToken);
        return new CliActionResponse(result, message: result.Data?.General.Environment);
    }

    [Action("log-level")]
    public static async Task<CliActionResponse> GetLogLevel(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service", Method.Get);
        var result = await RestProxy.Invoke<AppSettingsInfo>(restRequest, cancellationToken);
        return new CliActionResponse(result, message: result.Data?.General.LogLevel);
    }

    [Action("calendars")]
    public static async Task<CliActionResponse> GetAllCalendars(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service/calendars", Method.Get);
        var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
        var table = CliTableExtensions.GetCalendarsTable(result.Data);
        return new CliActionResponse(result, table);
    }

    [Action("login")]
    public static async Task<CliActionResponse> Login(CliLoginRequest request, CancellationToken cancellationToken = default)
    {
        var notnullRequest = FillLoginRequest(request);
        if (request.Port == 0) { request.Port = ConnectUtil.GetDefaultPort(); }
        var response = await InnerLogin(notnullRequest, cancellationToken);
        if (response.Response.IsSuccessful)
        {
            ConnectUtil.SaveLoginRequest(request, LoginProxy.Token);
        }

        return response;
    }

    [Action("logout")]
    public static async Task<CliActionResponse> Logout(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LoginProxy.Logout();
        ConnectUtil.Logout();
        RestProxy.Flush();
        return await Task.FromResult(CliActionResponse.Empty);
    }

    [Action("flush-logins")]
    public static async Task<CliActionResponse> FlushLogins(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectUtil.Flush();
        ConnectUtil.SetColor(CliColors.Default);
        return await Task.FromResult(CliActionResponse.Empty);
    }

    [Action("login-color")]
    public static async Task<CliActionResponse> LoginColor(CliLoginColorRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Color == null)
        {
            var colorType = typeof(CliColors);
            var options = CliActionMetadata.GetEnumOptions(colorType);
            var colorText = PromptSelection(options, "color");
            var parse = CliArgumentsUtil.ParseEnum(colorType, colorText);
            if (parse != null)
            {
                request.Color = (CliColors)parse;
            }
        }

        ConnectUtil.SetColor(request.Color.GetValueOrDefault());
        return await Task.FromResult(CliActionResponse.Empty);
    }

    [Action("security-audits")]
    public static async Task<CliActionResponse> GetSecurityAudits(CliGetSecurityAuditsRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("service/security-audits", Method.Get);

        if (request.FromDate > DateTime.MinValue)
        {
            restRequest.AddQueryParameter("fromDate", request.FromDate);
        }

        if (request.ToDate > DateTime.MinValue)
        {
            restRequest.AddQueryParameter("toDate", request.ToDate);
        }

        restRequest.AddQueryParameter("ascending", request.Ascending);
        restRequest.AddQueryPagingParameter(request);

        var result = await RestProxy.Invoke<PagingResponse<SecurityAuditModel>>(restRequest, cancellationToken);
        var table = CliTableExtensions.GetTable(result.Data);
        return new CliActionResponse(result, table);
    }

    [Action("working-hours")]
    public static async Task<CliActionResponse> GetWorkingHours(CliGetWorkingHoursRequest request, CancellationToken cancellationToken = default)
    {
        WorkingHoursModel? data = null;
        RestResponse response;

        if (string.IsNullOrEmpty(request.Calendar))
        {
            var restRequest = new RestRequest("service/working-hours", Method.Get);
            var result = await RestProxy.Invoke<IEnumerable<WorkingHoursModel>>(restRequest, cancellationToken);
            response = result;
            if (result.Data == null) { return new CliActionResponse(response); }

            if (result.Data.Count() > 1)
            {
                var calendar = PromptSelection(result.Data.Select(r => r.CalendarName), "calendar");
                data = result.Data.FirstOrDefault(r => r.CalendarName == calendar);
            }
            else
            {
                data = result.Data.First();
            }
        }
        else
        {
            var restRequest = new RestRequest("service/working-hours/{calendar}", Method.Get);
            restRequest.AddUrlSegment("calendar", request.Calendar);
            var result = await RestProxy.Invoke<WorkingHoursModel>(restRequest, cancellationToken);
            response = result;
            data = result.Data;
        }

        var table = CliTableExtensions.GetTable(data);
        return new CliActionResponse(response, table);
    }

    [Action("create-cryptography-key")]
    public static async Task<CliActionResponse> CreateCryptographyKey(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = Aes256Cipher.GenerateKey();
        var table = CliTableExtensions.GetTable(key);
        var response = CliActionResponse.GetGenericSuccessRestResponse();
        return await Task.FromResult(new CliActionResponse(response, table));
    }

    private const string encryptKey = "encrypted:";

    [Action("encrypt-settings")]
    public static async Task<CliActionResponse> EncryptSettings(CliEncryptAppsettingsRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Filename));
        cancellationToken.ThrowIfCancellationRequested();
        var filename = request.Filename ?? string.Empty;
        var text = await File.ReadAllTextAsync(filename, cancellationToken);
        if (text.StartsWith(encryptKey))
        {
            throw new CliException("file already encrypted");
        }

        var key = Cryptographic(request);
        var aes = new Aes256Cipher(key);
        var encrypted = $"{encryptKey}{aes.Encrypt(text)}";
        await File.WriteAllTextAsync(filename, encrypted, cancellationToken);
        var response = CliActionResponse.GetGenericSuccessRestResponse();
        return await Task.FromResult(new CliActionResponse(response));
    }

    [Action("decrypt-settings")]
    public static async Task<CliActionResponse> DecryptSettings(CliEncryptAppsettingsRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Filename));
        cancellationToken.ThrowIfCancellationRequested();
        var filename = request.Filename ?? string.Empty;
        var text = await File.ReadAllTextAsync(filename, cancellationToken);
        if (!text.StartsWith(encryptKey))
        {
            throw new CliException("file is not encrypted");
        }

        var key = Cryptographic(request);
        text = text[encryptKey.Length..];
        var aes = new Aes256Cipher(key);
        var decrypted = aes.Decrypt(text);
        await File.WriteAllTextAsync(filename, decrypted, cancellationToken);
        var response = CliActionResponse.GetGenericSuccessRestResponse();
        return await Task.FromResult(new CliActionResponse(response));
    }

    private static string Cryptographic(CliEncryptAppsettingsRequest request)
    {
        var fi = new FileInfo(request.Filename ?? string.Empty);
        if (!fi.Exists)
        {
            throw new CliException($"file {fi.FullName} not found");
        }

        if (!fi.Extension.Equals(".yml", StringComparison.OrdinalIgnoreCase))
        {
            throw new CliException($"file {fi.FullName} is not yml file");
        }

        var key = Environment.GetEnvironmentVariable(Consts.CryptographyKeyVariableKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new CliException(
                $"""
                    Environment variable {Consts.CryptographyKeyVariableKey} not found.
                    You can create new key with cli command: service create-cryptography-key
                    Then set the key in environment variable (i.e. setx {Consts.CryptographyKeyVariableKey} <generated_key>)
                    """
                );
        }

        return key;
    }

    public static async Task InitializeLogin()
    {
        var request = ConnectUtil.GetLastLoginRequestWithRemember();
        if (request == null)
        {
            SetDefaultAnonymousLogin();
            Console.Title = $"{CliConsts.Title} ({CliConsts.Anonymous})";
        }
        else
        {
            var response = await InnerLogin(request);
            if (!response.Response.IsSuccessful && response.Response.StatusCode != HttpStatusCode.Conflict)
            {
                RestProxy.Host = request.Host;
                RestProxy.Port = request.Port;
                RestProxy.SecureProtocol = request.SecureProtocol;
                RestProxy.Flush();
            }
        }
    }

    private static void SetDefaultAnonymousLogin()
    {
        ConnectUtil.Current.Host = RestProxy.Host;
        ConnectUtil.Current.Port = RestProxy.Port;

        var savedItem = ConnectUtil.GetSavedLogin(ConnectUtil.Current.Key);

        if (savedItem == null)
        {
            ConnectUtil.SaveLoginRequest(ConnectUtil.Current, token: null);
        }
        else
        {
            RestProxy.SecureProtocol = savedItem.SecureProtocol;
            ConnectUtil.Current.Color = savedItem.Color;
            ConnectUtil.Current.SecureProtocol = savedItem.SecureProtocol;
        }
    }

    private static CliLoginRequest FillLoginRequest(CliLoginRequest? request)
    {
        const string regexTepmplate = "^((6553[0-5])|(655[0-2][0-9])|(65[0-4][0-9]{2})|(6[0-4][0-9]{3})|([1-5][0-9]{4})|([0-5]{0,5})|([0-9]{1,4}))$";

        request ??= new CliLoginRequest();
        if (!InteractiveMode)
        {
            if (request.Color == CliColors.Default)
            {
                var savedLogin = ConnectUtil.GetSavedLogin(request.Key);
                if (savedLogin != null)
                {
                    request.Color = savedLogin.Color;
                }
            }

            return request;
        }

        if (string.IsNullOrEmpty(request.Host))
        {
            request.Host = CollectCliValue("host", true, 1, 50, defaultValue: ConnectUtil.DefaultHost) ?? string.Empty;
        }

        if (request.Port == 0)
        {
            request.Port = int.Parse(CollectCliValue("port", true, 1, 5, regexTepmplate, "invalid port", ConnectUtil.GetDefaultPort().ToString()) ?? ConnectUtil.GetDefaultPort().ToString());
        }

        if (string.IsNullOrEmpty(request.Username))
        {
            request.Username = CollectCliValue("username", required: false, 2, 50);
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            request.Password = CollectCliValue("password", required: false, 2, 50, secret: true);
        }

        var savedItem = ConnectUtil.GetSavedLogin(request.Key);
        if (savedItem != null)
        {
            request.Color = savedItem.Color;
            request.SecureProtocol = savedItem.SecureProtocol;
        }

        return request;
    }

    private static async Task<CliActionResponse> InnerLogin(CliLoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await LoginProxy.Login(request, cancellationToken);

        // Success authorize
        if (result.IsSuccessStatusCode)
        {
            Console.Title = $"{CliConsts.Title} ({request.Username})";
            _ = JobTriggerIdResolver.Refresh();
            return new CliActionResponse(result, message: $"login success ({LoginProxy.Role?.ToLower()})");
        }
        else if (result.StatusCode == HttpStatusCode.Conflict)
        {
            // No need to authorize
            RestProxy.Host = request.Host;
            RestProxy.Port = request.Port;
            RestProxy.SecureProtocol = request.SecureProtocol;
            RestProxy.Flush();

            LoginProxy.Logout();
            return CliActionResponse.Empty;
        }

        // Login error
        return new CliActionResponse(result);
    }
}