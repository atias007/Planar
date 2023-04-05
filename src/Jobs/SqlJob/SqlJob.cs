using CommonJob;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;
using SqlJob;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace Planar
{
    public abstract class SqlJob : BaseCommonJob<SqlJob, SqlJobProperties>
    {
        private readonly List<SqlJobException> _exceptions = new();

        protected SqlJob(ILogger<SqlJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Initialize(context);
                ValidateSqlJob();
                await ExecuteSql(context);
            }
            catch (Exception ex)
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.UnhandleException = ex;
            }
            finally
            {
                FinalizeJob(context);
            }
        }

        private async Task ExecuteSql(IJobExecutionContext context)
        {
            if (Properties.Steps == null)
            {
                Properties.Steps = new List<SqlStep>();
            }

            var total = Properties.Steps.Count;
            MessageBroker.AppendLog(LogLevel.Information, $"Start sql job with {total} steps");
            var isDefaultConnection =
                !string.IsNullOrWhiteSpace(Properties.ConnectionString) &&
                Properties.Steps.Any(s => string.IsNullOrWhiteSpace(s.ConnectionString));

            DbConnection? defaultConnection = null;
            DbTransaction? transaction = null;

            try
            {
                if (isDefaultConnection)
                {
                    defaultConnection = new SqlConnection(Properties.ConnectionString);
                    await defaultConnection.OpenAsync(context.CancellationToken);
                    if (Properties.Transaction)
                    {
                        var isolation = Properties.IsolationLevel ?? IsolationLevel.Unspecified;
                        transaction = await defaultConnection.BeginTransactionAsync(isolation, context.CancellationToken);
                        MessageBroker.AppendLog(LogLevel.Information, @"Begin transaction with isolation level {isolation}");
                    }
                }
                var counter = 0;

                foreach (var step in Properties.Steps)
                {
                    await ExecuteSqlStep(context, step, defaultConnection, transaction);
                    counter++;
                    var progress = Convert.ToByte(counter * 100.0 / total);
                    MessageBroker.UpdateProgress(progress);
                }

                if (_exceptions.Any())
                {
                    throw new AggregateException("There is one or more error(s) in sql job steps. See inner exceptions for more details", _exceptions);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                    MessageBroker.AppendLog(LogLevel.Information, "Commit transaction");
                }
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                    MessageBroker.AppendLog(LogLevel.Warning, "Rollback transaction due to error in one of the steps");
                }
                throw;
            }
            finally
            {
                try { transaction?.Dispose(); } catch { DoNothingMethod(); }
                try { defaultConnection?.Close(); } catch { DoNothingMethod(); }
                try { defaultConnection?.Dispose(); } catch { DoNothingMethod(); }
            }
        }

        private async Task ExecuteSqlStep(IJobExecutionContext context, SqlStep step, DbConnection? defaultConnection, DbTransaction? transaction)
        {
            DbConnection connection;
            var finalizeConnection = false;
            if (string.IsNullOrWhiteSpace(step.ConnectionString))
            {
                if (defaultConnection == null) { throw new SqlJobException($"No connection string defined for step name '{step.Name}' and no default connection"); }
                connection = defaultConnection;
            }
            else
            {
                finalizeConnection = true;
                connection = new SqlConnection(step.ConnectionString);
                await connection.OpenAsync(context.CancellationToken);
            }

            try
            {
                MessageBroker.AppendLog(LogLevel.Information, $"Start execute step name '{step.Name}'...");

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
                var rows = await cmd.ExecuteNonQueryAsync(context.CancellationToken);
                timer.Stop();
                var elapsedTitle =
                    timer.ElapsedMilliseconds < 60000 ?
                    $"{timer.Elapsed.Seconds}.{timer.Elapsed.Milliseconds:000}ms" :
                    $"{timer.Elapsed:hh\\:mm\\:ss}";

                MessageBroker.AppendLog(LogLevel.Information, $"Step name '{step.Name}' executed with {rows} effected row(s). Elapsed: {elapsedTitle}");
                MessageBroker.IncreaseEffectedRows(rows);
            }
            catch (Exception ex)
            {
                var sqlEx = new SqlJobException($"Fail to execute step name '{step.Name}'", ex);
                _logger.LogError(ex, "Fail to execute step name '{Name}'", step.Name);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail to execute step name '{step.Name}'. {ex.Message}");
                if (Properties.ContinueOnError)
                {
                    _exceptions.Add(sqlEx);
                }
                else
                {
                    throw sqlEx;
                }
            }
            finally
            {
                if (finalizeConnection)
                {
                    try { connection?.Close(); } catch { DoNothingMethod(); }
                    try { connection?.Dispose(); } catch { DoNothingMethod(); }
                }
            }
        }

        private string GetScript(IJobExecutionContext context, SqlStep step)
        {
            var result = step.Script;
            if (string.IsNullOrEmpty(result))
            {
                MessageBroker.AppendLog(LogLevel.Warning, $"Script filename '{step.Filename}' in step '{step.Name}' has no content");
                return string.Empty;
            }

            foreach (var item in context.MergedJobDataMap)
            {
                var key = $"{{{{{item.Key}}}}}";
                var value = Convert.ToString(item.Value);
                if (step.Script.Contains(key))
                {
                    result = result.Replace(key, value);
                    MessageBroker.AppendLog(LogLevel.Information, $"  - Placeholder '{key}' was replaced by value '{value}'");
                }
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                MessageBroker.AppendLog(LogLevel.Warning, $"Script filename '{step.Filename}' in step '{step.Name}' has no content after placeholder replace");
            }

            return result;
        }

        private void ValidateSqlJob()
        {
            try
            {
                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                Properties.Steps?.ForEach(s => ValidateSqlStep(s));
            }
            catch (Exception ex)
            {
                var source = nameof(ValidateSqlJob);
                _logger.LogError(ex, "Fail at {Source}", source);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }

        private void ValidateSqlStep(SqlStep step)
        {
            try
            {
                ValidateMandatoryString(step.Filename, nameof(step.Filename));
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
                _logger.LogError(ex, "Fail at {Source}", source);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }
    }
}