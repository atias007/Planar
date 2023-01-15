using Microsoft.Extensions.Configuration;
using Planar.Service;
using Planar.Service.Exceptions;
using System;
using System.Threading;

namespace Planar.Startup
{
    public static class AppSettingsInitializer
    {
        public static void Initialize()
        {
            var file1 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.json");
            var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{AppSettings.Environment}.json");

            IConfiguration config = null;

            try
            {
                Console.WriteLine("[x] Read AppSettings files");

                config = new ConfigurationBuilder()
                    .AddJsonFile(file1, optional: false, reloadOnChange: true)
                    .AddJsonFile(file2, optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fail to read application settings:");
                Console.WriteLine(ex.Message);
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }

            try
            {
                AppSettings.Initialize(config);
            }
            catch (AppSettingsException ex)
            {
                Console.WriteLine("Fail to initialize application settings:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                Console.WriteLine(ex.ToString());
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }

        public static void TestDatabaseConnection()
        {
            AppSettings.TestConnectionString();
        }

        public static void TestDatabasePermission()
        {
            AppSettings.TestDatabasePermission();
        }
    }
}