using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Planar.CLI.DataProtect
{
    public static class ConnectUtil
    {
        public const string DefaultHost = "localhost";
        public const int DefaultPort = 2306;
        public const int DefaultSecurePort = 2610;

        static ConnectUtil()
        {
            InitializeMetadataFolder();
            Load();
        }

        public static CliLoginRequest Current { get; private set; } = new CliLoginRequest();

        private static UserMetadata Data { get; set; } = new();

        private static string MetadataFilename { get; set; } = string.Empty;

        public static CliLoginRequest? GetSavedLoginRequestWithCredentials()
        {
            try
            {
                LogoutOldItems();
                var last = Data.Logins
                    .Where(l => l.HasCredentials)
                    .OrderByDescending(l => l.ConnectDate)
                    .FirstOrDefault();

                if (last == null) { return null; }

                var result = Map(last);
                Current = result;
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }
        }

        public static LoginData? GetSavedLogin(string key)
        {
            try
            {
                LogoutOldItems();
                var last = Data.Logins
                    .Where(l => l.Key == key)
                    .OrderByDescending(l => l.ConnectDate)
                    .FirstOrDefault();

                return last;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }
        }

        public static void Flush()
        {
            Data.Logins.Clear();
            Save();
        }

        public static void Logout()
        {
            Current.Username = null;
            Current.Password = null;

            var login = Data.Logins.FirstOrDefault(l => l.Key == Current.Key);
            if (login != null)
            {
                login.Token = null;
                login.Username = null;
                login.Password = null;
            }

            Save();
        }

        public static void Logout(LoginData login)
        {
            login.Username = null;
            login.Password = null;
            login.Token = null;
        }

        public static void SaveLoginRequest(CliLoginRequest request, string? token)
        {
            try
            {
                if (!request.Remember)
                {
                    request.Username = null;
                    request.Password = null;
                }

                Current = request;

                var login = ReverseMap(request);
                login.Token = token;
                Data.Logins.RemoveAll(l => l.Key == request.Key);

                if (request.Remember)
                {
                    var clear = Data.Logins.Where(l => l.Remember).ToList();
                    clear.ForEach(l =>
                    {
                        l.Remember = false;
                        l.RememberDays = null;
                    });
                }

                Data.Logins.Add(login);

                Save();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void LogoutOldItems()
        {
            var old = Data.Logins.Where(l => l.Deprecated).ToList();
            old.ForEach(o => Logout(o));
            Save();
        }

        private static CliLoginRequest Map(LoginData data)
        {
            var result = new CliLoginRequest
            {
                Color = data.Color,
                Host = data.Host,
                Password = data.Password,
                Port = data.Port,
                Username = data.Username,
                Remember = data.Remember,
                RememberDays = data.RememberDays.GetValueOrDefault(),
                SecureProtocol = data.SecureProtocol
            };

            return result;
        }

        private static LoginData ReverseMap(CliLoginRequest data)
        {
            var result = new LoginData
            {
                Color = data.Color,
                Host = data.Host,
                Password = data.Password,
                Port = data.Port,
                Username = data.Username,
                Remember = data.Remember,
                RememberDays = data.RememberDays,
                SecureProtocol = data.SecureProtocol,
                ConnectDate = DateTimeOffset.Now.DateTime
            };

            return result;
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
            AnsiConsole.MarkupLine($"{CliFormat.GetWarningMarkup("fail to read/write saved logins info")}");
            AnsiConsole.MarkupLine($"[{CliFormat.WarningColor}]exception message: {ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine(string.Empty.PadLeft(80, '-'));
            AnsiConsole.WriteException(ex);
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(MetadataFilename)) { return; }
                var text = File.ReadAllText(MetadataFilename);
                var protector = GetProtector();
                text = protector.Unprotect(text);
                Data = JsonConvert.DeserializeObject<UserMetadata>(text) ?? new UserMetadata();
                if (Data.Logins == null) { Data.Logins = new List<LoginData>(); }
                LogoutOldItems();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void InitializeMetadataFolder()
        {
            const string filename = "metadata.dat";
            var dataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(dataFolder, nameof(Planar));

            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            MetadataFilename = Path.Combine(folder, filename);
        }

        private static void Save()
        {
            try
            {
                var text = JsonConvert.SerializeObject(Data);
                var protector = GetProtector();
                text = protector.Protect(text);
                File.WriteAllText(MetadataFilename, text);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}