using CommonJob;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace Planar;

public abstract class SqlJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : BaseCommonJob<SqlJobProperties>(logger, dataLayer, jobMonitorUtil, clusterUtil)
{
    private readonly List<SqlJobException> _exceptions = [];

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            ValidateSqlJob();
            StartMonitorDuration(context);
            var task = ExecuteSql(context);
            await WaitForJobTask(context, task);
            StopMonitorDuration();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            await FinalizeJob(context);
        }
    }

    private async Task ExecuteSql(IJobExecutionContext context)
    {
        await Task.Yield();

        if (Properties.Steps == null)
        {
            Properties.Steps = [];
        }

        var total = Properties.Steps.Count;
        MessageBroker.AppendLog(LogLevel.Information, $"start sql job with {total} steps");
        var isOnlyDefaultConnection =
            !string.IsNullOrWhiteSpace(Properties.DefaultConnectionName) &&
            Properties.Steps.Exists(s => string.IsNullOrWhiteSpace(s.ConnectionName));

        DbConnection? defaultConnection = null;
        DbTransaction? transaction = null;

        try
        {
            if (isOnlyDefaultConnection)
            {
                defaultConnection = new SqlConnection(Properties.DefaultConnectionString);
                MessageBroker.AppendLog(LogLevel.Information, $"open default sql connection with connection name: {Properties.DefaultConnectionName}");
                await defaultConnection.OpenAsync(ExecutionCancellationToken);
                if (Properties.Transaction)
                {
                    var isolation = Properties.TransactionIsolationLevel ?? IsolationLevel.Unspecified;
                    transaction = await defaultConnection.BeginTransactionAsync(isolation, ExecutionCancellationToken);
                    MessageBroker.AppendLog(LogLevel.Information, $"begin transaction with isolation level {isolation}");
                }
            }
            var counter = 0;

            foreach (var step in Properties.Steps)
            {
                await ExecuteSqlStep(context, step, defaultConnection, transaction, ExecutionCancellationToken);
                counter++;
                var progress = Convert.ToByte(counter * 100.0 / total);

                try
                {
                    MessageBroker.UpdateProgress(progress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fail to update job progress");
                }
            }

            if (_exceptions.Count != 0)
            {
                throw new AggregateException("there is one or more error(s) in sql job steps. See inner exceptions for more details", _exceptions);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(ExecutionCancellationToken);
                MessageBroker.AppendLog(LogLevel.Information, "commit transaction");
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
                MessageBroker.AppendLog(LogLevel.Warning, "rollback transaction due to error in one of the steps");
            }
            throw;
        }
        finally
        {
            await SafeInvoke(async () => { if (transaction != null) { await transaction.DisposeAsync(); } });
            await SafeInvoke(async () => { if (defaultConnection != null) { await defaultConnection.CloseAsync(); } });
            await SafeInvoke(async () => { if (defaultConnection != null) { await defaultConnection.DisposeAsync(); } });
        }
    }

    private async Task ExecuteSqlStep(IJobExecutionContext context, SqlStep step, DbConnection? defaultConnection, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        var tuple = await GetDbConnection(step, defaultConnection, cancellationToken);
        DbConnection connection = tuple.Item1;
        var finalizeConnection = tuple.Item2;

        try
        {
            MessageBroker.AppendLog(LogLevel.Information, $"start execute step name '{step.Name}'...");

            DbCommand cmd = connection.CreateCommand();
            var script = GetScript(context, step);
            cmd.CommandText = script;
            cmd.CommandType = CommandType.Text;
            if (step.Timeout != null)
            {
                cmd.CommandTimeout = Convert.ToInt32(Math.Floor(step.Timeout.Value.TotalSeconds));
            }

            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            var timer = new Stopwatch();
            timer.Start();
            var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            timer.Stop();
            var elapsedTitle =
                timer.ElapsedMilliseconds < 60000 ?
                $"{timer.Elapsed.Seconds}.{timer.Elapsed.Milliseconds:000}ms" :
                $"{timer.Elapsed:hh\\:mm\\:ss}";

            MessageBroker.AppendLog(LogLevel.Information, $"step name '{step.Name}' executed with {rows} effected row(s). Elapsed: {elapsedTitle}");
            MessageBroker.IncreaseEffectedRows(rows);
        }
        catch (Exception ex)
        {
            HandleStepException(step, ex);
        }
        finally
        {
            if (finalizeConnection)
            {
                await SafeInvoke(async () => { if (connection != null) { await connection.CloseAsync(); } });
                await SafeInvoke(async () => { if (connection != null) { await connection.DisposeAsync(); } });
            }
        }
    }

    private async Task<Tuple<DbConnection, bool>> GetDbConnection(SqlStep step, DbConnection? defaultConnection, CancellationToken cancellationToken)
    {
        DbConnection? connection = null;
        var finalizeConnection = false;
        try
        {
            if (string.IsNullOrWhiteSpace(step.ConnectionString))
            {
                if (defaultConnection == null) { throw new SqlJobException($"no connection string defined for step name '{step.Name}' and no default connection"); }
                connection = defaultConnection;
            }
            else
            {
                finalizeConnection = true;
                connection = new SqlConnection(step.ConnectionString);
                MessageBroker.AppendLog(LogLevel.Information, $"open sql connection with connection name: {step.ConnectionName}");
                await connection.OpenAsync(cancellationToken);
            }

            return new Tuple<DbConnection, bool>(connection, finalizeConnection);
        }
        catch (Exception)
        {
            await SafeInvoke(async () => { if (connection != null) { await connection.DisposeAsync(); } });
            throw;
        }
    }

    private string GetScript(IJobExecutionContext context, SqlStep step)
    {
        var result = step.Script;
        if (string.IsNullOrEmpty(result))
        {
            MessageBroker.AppendLog(LogLevel.Warning, $"script filename '{step.Filename}' in step '{step.Name}' has no content");
            return string.Empty;
        }

        foreach (var item in context.MergedJobDataMap)
        {
            var key = $"{{{{{item.Key}}}}}";
            var value = Convert.ToString(item.Value);
            if (step.Script.Contains(key))
            {
                result = result.Replace(key, value);
                MessageBroker.AppendLog(LogLevel.Information, $"  - placeholder '{key}' was replaced by value '{value}'");
            }
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            MessageBroker.AppendLog(LogLevel.Warning, $"script filename '{step.Filename}' in step '{step.Name}' has no content after placeholder replace");
        }

        return result;
    }

    private void HandleStepException(SqlStep step, Exception ex)
    {
        var sqlEx = new SqlJobException($"fail to execute step name '{step.Name}'", ex);
        _logger.LogError(ex, "fail to execute step name {Name}", step.Name);
        MessageBroker.AppendLog(LogLevel.Error, $"fail to execute step name '{step.Name}'. {ex.Message}");
        if (Properties.ContinueOnError)
        {
            _exceptions.Add(sqlEx);
        }
        else
        {
            throw sqlEx;
        }
    }

    private string? ValidateConnectionName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) { return null; }

        var settingsKey = Settings.Keys
            .FirstOrDefault(k =>
                string.Equals(k, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(k, $"ConnectionStrings:{name}", StringComparison.OrdinalIgnoreCase))
            ?? throw new SqlJobException($"connection string name '{name}' could not be found in global config");

        var value = Settings[settingsKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SqlJobException($"connection string name '{name}' in global config has null or empty value");
        }

        return value;
    }

    private void ValidateSqlJob()
    {
        try
        {
            Properties.DefaultConnectionString = ValidateConnectionName(Properties.DefaultConnectionName);
            Properties.Steps?.ForEach(ValidateSqlStep);
        }
        catch (Exception ex)
        {
            var source = nameof(ValidateSqlJob);
            _logger.LogError(ex, "fail at {Source}", source);
            MessageBroker.AppendLog(LogLevel.Error, $"fail at {source}. {ex.Message}");
            throw new SqlJobException($"fail at {source}", ex);
        }
    }

    private void ValidateSqlStep(SqlStep step)
    {
        try
        {
            ValidateMandatoryString(step.Filename, nameof(step.Filename));
            step.ConnectionString = ValidateConnectionName(step.ConnectionName);
            step.FullFilename = FolderConsts.GetSpecialFilePath(
                PlanarSpecialFolder.Jobs,
                Properties.Path ?? string.Empty,
                step.Filename ?? string.Empty);

            if (!File.Exists(step.FullFilename))
            {
                throw new SqlJobException($"step '{step.Name}' filename '{step.FullFilename}' could not be found");
            }

            step.Script = File.ReadAllText(step.FullFilename, encoding: Encoding.UTF8);
        }
        catch (Exception ex)
        {
            var source = nameof(ValidateSqlStep);
            _logger.LogError(ex, "fail at {Source}", source);
            MessageBroker.AppendLog(LogLevel.Error, $"fail at {source}. {ex.Message}");
            throw new SqlJobException($"fail at {source}", ex);
        }
    }
}