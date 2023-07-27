using DatabaseMigrations;
using DbUp.Engine;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Status = DatabaseMigrations.Status;

var counter = 1000;
while (counter > 0)
{
    Run(args);
    Current.Status = Status.Success;
    Console.Clear();
    counter--;
}

static void Run(string[] args)
{
    var mode = GetRunningMode(args);

    if (mode == RunningMode.Validate)
    {
        Validate();
        AssertStatus();
        return;
    }
    else if (mode == RunningMode.AddScript)
    {
        AddScript();
        AssertStatus();
        return;
    }

#if DEBUG
    var environment = RunningEnvironment.Local;
#else
    var environment = RunningEnvironment.Test;
#endif

    var connectionString = GetConnectionString(environment);
    if (mode == RunningMode.EnsureDatabase)
    {
        Runner.EnsureDatabaseExists(connectionString);
        return;
    }

    if (mode == RunningMode.ListScripts)
    {
        Console.WriteLine();
        var list = Runner.GetScripts(connectionString);
        foreach (var item in list)
        {
            WriteInfo(item);
        }

        AssertStatus();
        return;
    }

    DatabaseUpgradeResult? result = null;
    switch (mode)
    {
        case RunningMode.DemoExecute:
            Validate();
            if (Current.Status == Status.Error)
            {
                AssertStatus();
                return;
            }

            Current.Status = Status.Success;
            result = Runner.DemoExecute(connectionString);
            break;

        case RunningMode.Execute:
            if (Current.Status != Status.Success)
            {
                AssertStatus();
                return;
            }

            result = Runner.Execute(connectionString);
            break;

        default:
            return;
    }

    if (!result.Successful)
    {
        WriteError(result.Error.ToString());
    }

    AssertStatus();
}
static RunningMode GetRunningMode(string[] args)
{
    string modeText;
#if DEBUG
    var modes = new[] { "Ensure Database", "Add Script", "Validate", "List Scripts", "Demo Execute", "Execute" };
#else
    var modes = new[] { "Ensure Database", "List Scripts", "Demo Execute", "Execute" };
#endif
    if (args.FirstOrDefault() == null)
    {
        var rule = new Rule("Choose running mode")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);

        modeText = AnsiConsole.Prompt(
        new SelectionPrompt<string>().AddChoices(modes));
    }
    else
    {
        modeText = args[0];
    }

    var success = Enum.TryParse<RunningMode>(modeText.Replace(" ", string.Empty), out var mode);
    if (!success)
    {
        WriteError($"argument '{modeText}' is not valid running mode");
    }

    Console.Clear();

    return mode;
}

static void Validate()
{
    ValidateAllSqlFiles();
    ValidateAllEbbdedResource();
}

static void ValidateAllEbbdedResource()
{
    Console.WriteLine(" [x] validate all script files is embedded resource");
    var files = Directory.GetFiles(Current.ProjectPath, "*.sql", SearchOption.AllDirectories);
    var filesList = files
        .Select(f => new
        {
            ResourceName = "DatabaseMigrations" + f.Replace(Current.ProjectPath, string.Empty).Replace(@"\", "."),
            Filename = f
        })
        .OrderBy(f => f.ResourceName)
        .ToList();

    var resources = Runner.ScriptAssembly
        .GetManifestResourceNames()
        .OrderBy(f => f)
        .ToList();

    var diff = filesList.Select(f => f.ResourceName).Except(resources).ToList();

    if (diff.Any())
    {
        diff.ForEach(f =>
        {
            var filename = filesList.Where(fl => fl.ResourceName == f).Select(f => f.Filename).First();
            var fi = new FileInfo(filename).Name;
            WriteError($"{fi} is not embedded resource");
        });
    }
}

static void ValidateAllSqlFiles()
{
    if (!Directory.Exists(Current.ScriptsPath))
    {
        WriteError("Folder 'Scripts' does not exists in project");
        return;
    }

    Console.WriteLine(" [x] validate all files is .sql extension");
    var files = Directory.GetFiles(Current.ScriptsPath, "*.*", SearchOption.AllDirectories)
        .Select(f => new FileInfo(f))
        .Where(f => !f.Extension.Equals(".sql", StringComparison.CurrentCultureIgnoreCase))
        .ToList();

    if (files.Any())
    {
        files.ForEach(f =>
        {
            WriteError($"{f.Name} is not .sql file");
        });
    }
}

static string GetConnectionString(RunningEnvironment environment)
{
    var builder = new ConfigurationBuilder();
    builder.AddJsonFile("appsettings.json");
    var configuration = builder.Build();

    var connectionString = configuration[environment.ToString()];
    if (string.IsNullOrEmpty(connectionString))
    {
        WriteError($"Could not found connection with name '{environment}'");
        AssertStatus();
        connectionString = string.Empty;
    }

    return connectionString;
}

static void AddScript()
{
    var name = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter script name [gray](between 3 to 20 letters)[/]: ")
        .ValidationErrorMessage("[red]That's not a valid file name[/]")
        .Validate(name =>
        {
            const string regexTemplate = @"^([a-zA-Z0-9\s_\\.\-\(\):]){3,20}$";
            return Regex.IsMatch(name, regexTemplate, RegexOptions.None, TimeSpan.FromSeconds(5));
        }));

    name = name.Trim();
    if (!name.ToLower().EndsWith(".sql"))
    {
        name += ".sql";
    }

    var allFiles = Directory.GetFiles(Current.ScriptsPath, $"{Current.ModuleName}_*.*")
        .Select(f => new FileInfo(f).Name.Substring(Current.ModuleName.Length + 1, 4))
        .Where(f => int.TryParse(f, out _))
        .Select(f => int.Parse(f, CultureInfo.CurrentCulture))
        .ToList();

    var maxNumber = allFiles.Any() ? allFiles.Max() : 0;
    var nextNumber = (maxNumber + 1).ToString("000#", CultureInfo.CurrentCulture);
    var scriptFile = $"{Current.ModuleName}_{nextNumber} - {name}";
    var filename = Path.Combine(Current.ScriptsPath, scriptFile);

    try
    {
        File.WriteAllText(filename, string.Empty);
        Process.Start("cmd.exe ", "/c \"" + filename + "\"");
    }
    catch (Exception ex)
    {
        WriteError(ex.Message);
    }

    var csprojFilename = Path.Combine(Current.ProjectPath, "DatabaseMigrations.Factory.csproj");
    var csprojContent = File.ReadAllText(csprojFilename);
    var doc = XDocument.Parse(csprojContent);

    var itemsGroup = doc.Root!.Elements("ItemGroup");
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

    if (!handleNone)
    {
        var element = new XElement("ItemGroup");
        var subElement = new XElement("None");
        var attribute = new XAttribute("Remove", $@"Scripts\{scriptFile}");

        subElement.Add(attribute);
        element.Add(subElement);
        doc.Root.Add(element);
    }

    if (!handleEmbedded)
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

static void WriteError(string text)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"      --> {text}");
    Console.ResetColor();
    Current.Status = Status.Error;
}

static void WriteInfo(string text)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine($"- {text}");
    Console.ResetColor();
}

static void AssertStatus()
{
    Console.WriteLine();
    Console.WriteLine();

    if (Current.Status == Status.Success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
    }
    else if (Current.Status == Status.Error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }
    else if (Current.Status == Status.Warning)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
    }

    Console.WriteLine(Current.Status.ToString());
    Console.ResetColor();

    Exit();
}

static void Exit()
{
#if DEBUG
    Console.WriteLine("Press enter to close");
    Console.ReadLine();
#else
    Environment.Exit(Current.Status == Status.Success ? 0 : -1);
#endif
}