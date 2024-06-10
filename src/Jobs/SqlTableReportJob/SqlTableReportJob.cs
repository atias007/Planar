using CommonJob;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MimeKit;
using Planar.Common;
using Quartz;
using SqlTableReportJob;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace Planar;

internal record Attendee(string FirstName, string? LastName, string Email);

public abstract class SqlTableReportJob : BaseCommonJob<SqlTableReportJobProperties>
{
    private readonly IGroupDataLayer _groupData;

    protected SqlTableReportJob(
        ILogger logger,
        IJobPropertyDataLayer dataLayer,
        IGroupDataLayer groupData,
        JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
    {
        _groupData = groupData;
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            ValidateSqlJob();
            StartMonitorDuration(context);
            var task = Task.Run(() => Generate(context));
            await WaitForJobTask(context, task);
            StopMonitorDuration();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            FinalizeJob(context);
        }
    }

    private async Task Generate(IJobExecutionContext context)
    {
        DbConnection? connection = null;

        try
        {
            connection = new SqlConnection(Properties.ConnectionString);
            MessageBroker.AppendLog(LogLevel.Information, $"Open sql connection with connection name: {Properties.ConnectionName}");
            await connection.OpenAsync(context.CancellationToken);

            var attendees = await GetUsers(Properties.Group);
            var table = await ExecuteSql(context, Properties, connection);
            var html = GenerateHtml(Properties, table);
            html = HtmlUtil.MinifyHtml(html);
            await SendReport(html, attendees);
        }
        finally
        {
            try { if (connection != null) { await connection.CloseAsync(); } } catch { DoNothingMethod(); }
            try { if (connection != null) { await connection.DisposeAsync(); } } catch { DoNothingMethod(); }
        }
    }

    private async Task<DataTable> ExecuteSql(IJobExecutionContext context, SqlTableReportJobProperties properties, DbConnection connection)
    {
        try
        {
            var cmd = connection.CreateCommand();
            var script = GetScript(context, properties);
            cmd.CommandText = script;
            cmd.CommandType = CommandType.Text;
            if (properties.Timeout != null)
            {
                cmd.CommandTimeout = Convert.ToInt32(Math.Floor(properties.Timeout.Value.TotalSeconds));
            }

            var timer = new Stopwatch();
            timer.Start();
            using var reader = await cmd.ExecuteReaderAsync(context.CancellationToken);
            timer.Stop();

            var elapsedTitle =
                timer.ElapsedMilliseconds < 60000 ?
                $"{timer.Elapsed.Seconds}.{timer.Elapsed.Milliseconds:000}ms" :
                $"{timer.Elapsed:hh\\:mm\\:ss}";

            var table = ConvertDataReaderToDataTable(reader);
            var rows = table.Rows.Count;
            MessageBroker.AppendLog(LogLevel.Information, $"script executed with {rows} effected row(s). Elapsed: {elapsedTitle}");
            MessageBroker.IncreaseEffectedRows(rows);

            return table;
        }
        catch (Exception ex)
        {
            var sqlEx = new SqlTableReportJobException("Fail to execute script", ex);
            _logger.LogError(ex, "fail to execute script");
            MessageBroker.AppendLog(LogLevel.Error, $"Fail to execute script. {ex.Message}");
            throw sqlEx;
        }
    }

    private async Task<IEnumerable<Attendee>> GetUsers(string groupName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new SqlTableReportJobException("No distibution group is defined is this job");
            }

            var users = await GetUsersInner(groupName);
            if (!users.Any())
            {
                throw new SqlTableReportJobException($"No users with email in group '{groupName}'");
            }

            return users;
        }
        catch (Exception ex)
        {
            throw new SqlTableReportJobException($"Fail to get users of group '{groupName}'", ex);
        }
    }

    private async Task<IEnumerable<Attendee>> GetUsersInner(string groupName)
    {
        var result = new List<Attendee>();
        var users = await _groupData.GetGroupUsers(groupName);
        var list = users
            .Where(u => !string.IsNullOrWhiteSpace(u.EmailAddress1))
            .Select(u => new Attendee(u.FirstName, u.LastName, u.EmailAddress1 ?? string.Empty));
        result.AddRange(list);

        list = users
            .Where(u => !string.IsNullOrWhiteSpace(u.EmailAddress2))
            .Select(u => new Attendee(u.FirstName, u.LastName, u.EmailAddress2 ?? string.Empty));
        result.AddRange(list);

        list = users
           .Where(u => !string.IsNullOrWhiteSpace(u.EmailAddress3))
           .Select(u => new Attendee(u.FirstName, u.LastName, u.EmailAddress3 ?? string.Empty));
        result.AddRange(list);

        return result;
    }

    private async Task SendReport(string html, IEnumerable<Attendee> attendees)
    {
        try
        {
            var message = new MimeMessage();

            foreach (var recipient in attendees)
            {
                if (string.IsNullOrEmpty(recipient.Email)) { continue; }
                if (!IsValidEmail(recipient.Email))
                {
                    _logger.LogWarning("send sql table report warning: email address '{Email}' is not valid", recipient);
                }
                else
                {
                    message.Bcc.Add(new MailboxAddress($"{recipient.FirstName} {recipient.LastName}".Trim(), recipient.Email));
                }
            }

            message.Subject = $"Planar SQL Table Report | {Properties.Title} | {AppSettings.General.Environment}";
            var body = new BodyBuilder
            {
                HtmlBody = html,
            }.ToMessageBody();
            message.Body = body;

            var result = await SmtpUtil.SendMessage(message);
            _logger.LogDebug("SMTP send result: {Message}", result);
        }
        catch (Exception ex)
        {
            throw new SqlTableReportJobException("Fail to send report", ex);
        }
    }

    private static bool IsValidEmail(string? value)
    {
        if (value == null) { return true; }
        return Consts.EmailRegex.IsMatch(value);
    }

    private static string GenerateHtml(SqlTableReportJobProperties properties, DataTable table)
    {
        try
        {
            var result = ReportGenerator.Generate(table, properties.Title);
            return result;
        }
        catch (Exception ex)
        {
            throw new SqlTableReportJobException("Fail to generate html", ex);
        }
    }

    // convert reader to data table

    private string GetScript(IJobExecutionContext context, SqlTableReportJobProperties properties)
    {
        var result = properties.Script;
        if (string.IsNullOrEmpty(result))
        {
            MessageBroker.AppendLog(LogLevel.Warning, $"Script filename '{properties.Filename}' has no content");
            return string.Empty;
        }

        foreach (var item in context.MergedJobDataMap)
        {
            var key = $"{{{{{item.Key}}}}}";
            var value = Convert.ToString(item.Value);
            if (properties.Script.Contains(key))
            {
                result = result.Replace(key, value);
                MessageBroker.AppendLog(LogLevel.Information, $"  - Placeholder '{key}' was replaced by value '{value}'");
            }
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            MessageBroker.AppendLog(LogLevel.Warning, $"Script filename '{properties.Filename}' has no content after placeholder replace");
        }

        return result;
    }

    private string? ValidateConnectionName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) { return null; }

        var settingsKey = Settings.Keys
            .FirstOrDefault(k =>
                string.Equals(k, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(k, $"ConnectionStrings:{name}", StringComparison.OrdinalIgnoreCase))
            ?? throw new SqlTableReportJobException($"connection string name '{name}' could not be found in global config");

        var value = Settings[settingsKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SqlTableReportJobException($"connection string name '{name}' in global config has null or empty value");
        }

        return value;
    }

    private void ValidateSqlJob()
    {
        try
        {
            ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
            ValidateMandatoryString(Properties.Filename, nameof(Properties.Filename));
            ValidateMandatoryString(Properties.Group, nameof(Properties.Group));
            Properties.ConnectionString = ValidateConnectionName(Properties.ConnectionName);

            Properties.FullFilename = FolderConsts.GetSpecialFilePath(
                PlanarSpecialFolder.Jobs,
                Properties.Path ?? string.Empty,
                Properties.Filename ?? string.Empty);

            if (!File.Exists(Properties.FullFilename))
            {
                throw new SqlTableReportJobException($"filename '{Properties.FullFilename}' could not be found");
            }

            Properties.Script = File.ReadAllText(Properties.FullFilename, encoding: Encoding.UTF8);
        }
        catch (Exception ex)
        {
            var source = nameof(ValidateSqlJob);
            _logger.LogError(ex, "fail at {Source}", source);
            MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
            throw;
        }
    }

    private DataTable ConvertDataReaderToDataTable(DbDataReader reader)
    {
        var dataTable = new DataTable();

        // Create columns in the DataTable based on the reader's schema
        for (int i = 0; i < reader.FieldCount; i++)
        {
            dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
        }

        var counter = 0;
        // Read the data from the reader and populate the DataTable
        while (reader.Read())
        {
            DataRow dataRow = dataTable.NewRow();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                dataRow[i] = reader.GetValue(i);
            }

            dataTable.Rows.Add(dataRow);
            counter++;

            if (counter >= 1000)
            {
                _logger.LogWarning("max rows limit ({Counter}) reached", counter);
            }
        }

        return dataTable;
    }
}