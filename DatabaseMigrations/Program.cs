using DbUp;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DatabaseMigrations
{
    internal class Program
    {
        private static readonly string ProjectPath = new DirectoryInfo(Path.Combine(RunningPath, "..", "..", "..")).FullName;

        private static Status _status = Status.Success;

        public static string ModuleName { get; set; }

        private static string RunningPath => AppContext.BaseDirectory;

        private static string ScriptsPath => Path.Combine(ProjectPath, "Scripts");

        private static void AddScript()
        {
            var name = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter script name [gray](between 3 to 20 letters)[/]: ")
                .ValidationErrorMessage("[red]That's not a valid file name[/]")
                .Validate(name =>
                {
                    const string regexTemplate = @"^([a-zA-Z0-9\s_\\.\-\(\):]){3,20}$";
                    return Regex.IsMatch(name, regexTemplate);
                }));

            name = name.Trim();
            if (name.ToLower().EndsWith(".sql") == false)
            {
                name += ".sql";
            }

            var allFiles = Directory.GetFiles(ScriptsPath, $"{ModuleName}_*.*")
                .Select(f => new FileInfo(f).Name.Substring(ModuleName.Length + 1, 4))
                .Where(f => int.TryParse(f, out _))
                .Select(f => int.Parse(f, CultureInfo.CurrentCulture))
                .ToList();

            var maxNumber = allFiles.Any() ? allFiles.Max() : 0;
            var nextNumber = (maxNumber + 1).ToString("000#", CultureInfo.CurrentCulture);
            var scriptFile = $"{ModuleName}_{nextNumber} - {name}";
            var filename = Path.Combine(ScriptsPath, scriptFile);

            try
            {
                File.WriteAllText(filename, string.Empty);
                Process.Start("cmd.exe ", "/c \"" + filename + "\"");
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }

            var csprojFilename = Path.Combine(ProjectPath, "DatabaseMigrations.csproj");
            var csprojContent = File.ReadAllText(csprojFilename);
            var doc = XDocument.Parse(csprojContent);
            var itemsGroup = doc.Root.Elements("ItemGroup");
            var handleNone = false;
            var handleEmbedded = false;
            foreach (var item in itemsGroup)
            {
                var firstElement = item.Elements().FirstOrDefault();
                if (firstElement != null)
                {
                    if (firstElement.Name == "None")
                    {
                        var element = new XElement("None");
                        element.Add(new XAttribute("Remove", $@"Scripts\{scriptFile}"));
                        item.Add(element);
                        handleNone = true;
                    }
                    else if (firstElement.Name == "EmbeddedResource")
                    {
                        var element = new XElement("EmbeddedResource");
                        element.Add(new XAttribute("Include", $@"Scripts\{scriptFile}"));
                        item.Add(element);
                        handleEmbedded = true;
                    }
                }
            }

            if (handleNone == false)
            {
                var element = new XElement("ItemGroup");
                var subElement = new XElement("None");
                var attribute = new XAttribute("Remove", $@"Scripts\{scriptFile}");

                subElement.Add(attribute);
                element.Add(subElement);
                doc.Root.Add(element);
            }

            if (handleEmbedded == false)
            {
                var element = new XElement("ItemGroup");
                var subElement = new XElement("EmbeddedResource");
                var attribute = new XAttribute("Include", $@"Scripts\{scriptFile}");

                subElement.Add(attribute);
                element.Add(subElement);
                doc.Root.Add(element);
            }

            try
            {
                File.WriteAllText(csprojFilename, doc.ToString());
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }

        private static void AssertStatus()
        {
            Console.WriteLine();
            Console.WriteLine();

            if (_status == Status.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (_status == Status.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (_status == Status.Warning)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }

            Console.WriteLine(_status.ToString());
            Console.ResetColor();

            Exit();
        }

        private static void Exit()
        {
#if DEBUG
            Console.WriteLine("Press enter to close");
            Console.ReadLine();
#endif
            Environment.Exit(_status == Status.Success ? 0 : -1);
        }

        private static string GetConnectionString(RunningEnvironment environment)
        {
            var settingsFile = Path.Combine(RunningPath, $"appsettings.{environment}.json");
            if (File.Exists(settingsFile) == false)
            {
                WriteError($"Could not found appsettings.{ environment}.json file");
                AssertStatus();
            }

            var json = JObject.Parse(File.ReadAllText(settingsFile));
            var connectionString = json["DatabaseConnectionString"].ToString();

            if (string.IsNullOrEmpty(connectionString))
            {
                WriteError($"Could not found connection string with name start with '{ModuleName}'");
                AssertStatus();
            }

            return connectionString;
        }

        private static RunningEnvironment GetRunningEnvironment(string[] args)
        {
            string envText;

            if (args.FirstOrDefault() == null || args.Length < 2)
            {
                var rule = new Rule("Choose running environment")
                {
                    Alignment = Justify.Left
                };
                AnsiConsole.Write(rule);

                var members = Enum.GetNames(typeof(RunningEnvironment));
                envText = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices(members));
            }
            else
            {
                envText = args[1];
            }

            var success = Enum.TryParse<RunningEnvironment>(envText, out var env);
            if (success == false)
            {
                WriteError($"argument '{envText}' is not valid running environment");
            }

            Console.Clear();

            return env;
        }

        private static RunningMode GetRunningMode(string[] args)
        {
            string modeText;
            if (args.FirstOrDefault() == null)
            {
                var rule = new Rule("Choose running mode")
                {
                    Alignment = Justify.Left
                };
                AnsiConsole.Write(rule);

                modeText = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices(new[]
                    {
                        "Add Script",
                        "Validate",
                        "List Scripts",
                        "Demo Execute",
                        "Execute"
                    }));
            }
            else
            {
                modeText = args[0];
            }

            var success = Enum.TryParse<RunningMode>(modeText.Replace(" ", string.Empty), out var mode);
            if (success == false)
            {
                WriteError($"argument '{modeText}' is not valid running mode");
            }

            Console.Clear();

            return mode;
        }

        private static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            ModuleName = "Planner";
            Run(assembly, args);
        }

        private static void Run(Assembly assembly, string[] args)
        {
            if (string.IsNullOrEmpty(ModuleName))
            {
                ModuleName = new DirectoryInfo(Path.Combine(RunningPath, "..", "..", "..", "..")).Name;
            }

            var mode = GetRunningMode(args);

            if (mode == RunningMode.Validate)
            {
                Validate(assembly);
                AssertStatus();
            }
            else if (mode == RunningMode.AddScript)
            {
                AddScript();
                AssertStatus();
            }

            var environment = GetRunningEnvironment(args);
            var connectionString = GetConnectionString(environment);

            var builder =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(assembly)
                    .LogToConsole()
                    .LogScriptOutput();

            if (mode == RunningMode.ListScripts)
            {
                Console.WriteLine();

                builder.Build().GetScriptsToExecute()
                    .ForEach(s =>
                    {
                        WriteInfo(s.Name);
                    });

                AssertStatus();
            }

            switch (mode)
            {
                case RunningMode.DemoExecute:
                    Validate(assembly);
                    if (_status == Status.Error)
                    {
                        AssertStatus();
                    }

                    _status = Status.Success;
                    builder.WithTransactionAlwaysRollback();
                    break;

                case RunningMode.PullRequestReady:
                    Validate(assembly);
                    if (_status != Status.Success)
                    {
                        AssertStatus();
                    }

                    _status = Status.Success;
                    builder.WithTransactionAlwaysRollback();
                    break;

                case RunningMode.Execute:
                    Validate(assembly);
                    if (_status != Status.Success)
                    {
                        AssertStatus();
                    }

                    builder.WithTransaction();
                    break;
            }

            var upgrader = builder.Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                WriteError(result.Error.ToString());
            }

            AssertStatus();
        }

        private static void Validate(Assembly assembly)
        {
            ValidateAllSqlFiles();
            ValidateAllEbbdedResource(assembly);
        }

        private static void ValidateAllEbbdedResource(Assembly assembly)
        {
            Console.WriteLine(" [x] validate all script files is embedded resource");
            var files = Directory.GetFiles(ProjectPath, "*.sql", SearchOption.AllDirectories);
            var filesList = files
                .Select(f => new
                {
                    ResourceName = "DatabaseMigrations" + f.Replace(ProjectPath, string.Empty).Replace(@"\", "."),
                    Filename = f
                })
                .OrderBy(f => f.ResourceName)
                .ToList();

            var resources = assembly
                .GetManifestResourceNames()
                .OrderBy(f => f)
                .ToList();

            var diff = filesList.Select(f => f.ResourceName).Except(resources).ToList();

            if (diff.Any())
            {
                diff.ForEach(f =>
                {
                    var filename = filesList.Where(fl => fl.ResourceName == f).Select(f => f.Filename).FirstOrDefault();
                    var fi = new FileInfo(filename).Name;
                    WriteError($"{fi} is not embedded resource");
                });
            }
        }

        private static void ValidateAllSqlFiles()
        {
            if (Directory.Exists(ScriptsPath) == false)
            {
                WriteError("Folder 'Scripts' does not exists in project");
                return;
            }

            Console.WriteLine(" [x] validate all files is .sql extension");
            var files = Directory.GetFiles(ScriptsPath, "*.*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => f.Extension.Equals(".sql", StringComparison.CurrentCultureIgnoreCase) == false)
                .ToList();

            if (files.Any())
            {
                files.ForEach(f =>
                {
                    WriteError($"{f.Name} is not .sql file");
                });
            }
        }

        private static void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"      --> {text}");
            Console.ResetColor();
            _status = Status.Error;
        }

        private static void WriteInfo(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"- {text}");
            Console.ResetColor();
        }

        private static void WriteWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"      --> {text}");
            Console.ResetColor();

            if (_status != Status.Error)
            {
                _status = Status.Warning;
            }
        }
    }
}