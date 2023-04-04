using CommonJob;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;
using SqlJob;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Planar
{
    public abstract class SqlJob : BaseCommonJob<SqlJob, SqlJobProperties>
    {
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

            // TODO: log start and total steps
            var isDefaultConnection =
                !string.IsNullOrWhiteSpace(Properties.ConnectionString) &&
                Properties.Steps.Any(s => string.IsNullOrWhiteSpace(s.ConnectionString));

            SqlConnection? defaultConnection = null;
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
                    }
                }

                foreach (var step in Properties.Steps)
                {
                    await ExecuteSqlStep(context, step, defaultConnection);
                }

                transaction?.Commit();
            }
            catch
            {
                transaction?.Rollback();
            }
            finally
            {
                transaction?.Dispose();
                defaultConnection?.Close();
                defaultConnection?.Dispose();
            }
        }

        private async Task ExecuteSqlStep(IJobExecutionContext context, SqlStep step, SqlConnection? defaultConnection)
        {
            SqlConnection connection;
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
                var cmd = connection.CreateCommand();
                cmd.CommandText = step.Script;
                cmd.CommandType = CommandType.Text;
                if (step.Timeout != null)
                {
                    cmd.CommandTimeout = Convert.ToInt32(Math.Floor(step.Timeout.Value.TotalSeconds));
                }

                // TODO: replace parameters in script
                // TODO: log replaces variables
                var rows = await cmd.ExecuteNonQueryAsync(context.CancellationToken);
                // TODO: change all logger write
                _logger.LogInformation("step name '{Name}' executed with {Rows} effected rows", step.Name, rows);
                // TODO: log the execution time
            }
            catch (Exception ex)
            {
                var sqlEx = new SqlJobException($"Fail to execute step name '{step.Name}'", ex);
                _logger.LogError(ex, "fail to execute step name '{Name}'", step.Name);
                if (Properties.ContinueOnError)
                {
                    // TODO: add aggregate exception
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
                    connection?.Close();
                    connection?.Dispose();
                }
            }
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
                throw;
            }
        }
    }
}