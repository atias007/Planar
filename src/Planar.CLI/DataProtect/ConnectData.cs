using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Planar.CLI.DataProtect
{
    internal class ConnectData
    {
        private const string filename = "metadata.dat";

        static ConnectData()
        {
            Load();
        }

        private static Dictionary<string, CliLoginRequest> Data { get; set; } = new();

        public static CliLoginRequest GetLoginRequest()
        {
            try
            {
                var loginName = GetLoginKey();
                Data.TryGetValue(loginName, out var result);
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }
        }

        public static void SetLoginRequest(CliLoginRequest request)
        {
            try
            {
                var loginName = GetLoginKey();
                RemoveCurrentLogin(loginName);

                if (!request.Remember) { return; }

                request.ConnectDate = DateTimeOffset.Now.Date;

                if (Data.ContainsKey(loginName))
                {
                    Data[loginName] = request;
                }
                else
                {
                    Data.Add(loginName, request);
                }

                Save();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void FilterOldItems()
        {
            var oldItems = Data.Where(d => d.Value.ConnectDate.AddDays(d.Value.RememberDays) < DateTimeOffset.Now.Date);
            if (!oldItems.Any()) { return; }

            foreach (var item in oldItems)
            {
                Data.Remove(item.Key);
            }

            Save();
        }

        private static string GetLoginKey()
        {
            var loginName = $"{Environment.UserDomainName}|{Environment.UserName}";
            return loginName;
        }

        private static IDataProtector GetProtector()
        {
            const string purpose = "RememberConnect";
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            var provider = serviceCollection.BuildServiceProvider();
            var protector = provider.GetRequiredService<IDataProtectionProvider>();
            return protector.CreateProtector(purpose);
        }

        private static void HandleException(Exception ex)
        {
            AnsiConsole.WriteLine($"{CliTableFormat.WarningColor}fail to read/write saved logins info[/]");
            AnsiConsole.WriteLine($"{CliTableFormat.WarningColor}exception message: {ex.Message.EscapeMarkup()}[/]");
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(filename))
                {
                    return;
                }

                var text = File.ReadAllText(filename);
                var protector = GetProtector();
                text = protector.Unprotect(text);
                Data = JsonConvert.DeserializeObject<Dictionary<string, CliLoginRequest>>(text);
                FilterOldItems();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void RemoveCurrentLogin(string loginName)
        {
            if (Data.ContainsKey(loginName))
            {
                Data.Remove(loginName);
            }

            Save();
        }

        private static void Save()
        {
            try
            {
                var text = JsonConvert.SerializeObject(Data);
                var protector = GetProtector();
                text = protector.Protect(text);
                File.WriteAllText(filename, text);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}