﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Preprocessors;
using DbUp.Engine.Transactions;
using DbUp.Helpers;

namespace DbUp.Support
{
    /// <summary>
    /// A standard implementation of the IScriptExecutor interface that executes against a SQL Server
    /// database.
    /// </summary>
    public abstract class ScriptExecutor : IScriptExecutor
    {
        private readonly Func<IConnectionManager> connectionManagerFactory;
        private readonly IEnumerable<IScriptPreprocessor> scriptPreprocessors;
        private readonly Func<IJournal> journalFactory;
        private readonly Func<bool> variablesEnabled;
        private readonly ISqlObjectParser sqlObjectParser;

        /// <summary>
        /// SQLCommand Timeout in seconds. If not set, the default SQLCommand timeout is not changed.
        /// </summary>
        public int? ExecutionTimeoutSeconds { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="SqlScriptExecutor"/> class.
        /// </summary>
        /// <param name="connectionManagerFactory"></param>
        /// <param name="log">The logging mechanism.</param>
        /// <param name="schema">The schema that contains the table.</param>
        /// <param name="variablesEnabled">Function that returns <c>true</c> if variables should be replaced, <c>false</c> otherwise.</param>
        /// <param name="scriptPreprocessors">Script Preprocessors in addition to variable substitution</param>
        /// <param name="journalFactory">Database journal</param>
        protected ScriptExecutor(
            Func<IConnectionManager> connectionManagerFactory, ISqlObjectParser sqlObjectParser,
            Func<IUpgradeLog> log, string schema, Func<bool> variablesEnabled,
            IEnumerable<IScriptPreprocessor> scriptPreprocessors,
            Func<IJournal> journalFactory)
        {
            Schema = schema;
            Log = log;
            this.variablesEnabled = variablesEnabled;
            this.scriptPreprocessors = scriptPreprocessors;
            this.journalFactory = journalFactory;
            this.connectionManagerFactory = connectionManagerFactory;
            this.sqlObjectParser = sqlObjectParser;
        }

        /// <summary>
        /// Database Schema, should be null if database does not support schemas
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Verifies the existence of targeted schema. If schema is not verified, will check for the existence of the dbo schema.
        /// </summary>
        public void VerifySchema()
        {
            if (string.IsNullOrEmpty(Schema)) return;

            connectionManagerFactory().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
            {
                var sqlRunner = new AdHocSqlRunner(dbCommandFactory, sqlObjectParser, Schema, () => true);
                var sql = GetVerifySchemaSql(Schema);
                sqlRunner.ExecuteNonQuery(sql);
            });
        }

        protected abstract string GetVerifySchemaSql(string schema);

        /// <summary>
        /// Some database systems (e.g. Firebird) require that scripts run in a seperate transaction than those
        /// creating the JournalTable. The default is true but can be changed when inheriting ScriptExecutor
        /// </summary>
        protected virtual bool UseTheSameTransactionForJournalTableAndScripts => true;

        protected virtual string PreprocessScriptContents(SqlScript script, IDictionary<string, string> variables)
        {
            variables ??= new Dictionary<string, string>();
            if (Schema != null && !variables.ContainsKey("schema"))
                variables.Add("schema", QuoteSqlObjectName(Schema));

            var contents = script.Contents;
            if (string.IsNullOrEmpty(Schema))
                contents = new StripSchemaPreprocessor().Process(contents);
            if (variablesEnabled())
                contents = new VariableSubstitutionPreprocessor(variables).Process(contents);
            contents = (scriptPreprocessors ?? Array.Empty<IScriptPreprocessor>())
                .Aggregate(contents, (current, additionalScriptPreprocessor) => additionalScriptPreprocessor.Process(current));

            return contents;
        }

        /// <summary>
        /// Executes the specified script against a database at a given connection string.
        /// </summary>
        /// <param name="script">The script.</param>
        public virtual void Execute(SqlScript script)
        {
            Execute(script, null);
        }

        /// <summary>
        /// Executes the specified script against a database at a given connection string.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="variables">Variables to replace in the script</param>
        public virtual void Execute(SqlScript script, IDictionary<string, string> variables)
        {
            var contents = PreprocessScriptContents(script, variables);
            Log().WriteInformation("Executing Database Server script {0}", script.Name);

            var connectionManager = connectionManagerFactory();
            var scriptStatements = connectionManager.SplitScriptIntoCommands(contents);
            var index = -1;

            try
            {
                connectionManager.ExecuteCommandsWithManagedConnection(dbCommandFactory =>
                {
                    var journal = journalFactory();
                    journal.EnsureTableExistsAndIsLatestVersion(dbCommandFactory);

                    foreach (var statement in scriptStatements)
                    {
                        index++;
                        using var command = dbCommandFactory();
                        command.CommandText = statement;
                        if (ExecutionTimeoutSeconds != null)
                            command.CommandTimeout = ExecutionTimeoutSeconds.Value;

                        Action<IDbCommand> executeAction;
                        if (connectionManager.IsScriptOutputLogged)
                        {
                            executeAction = ExecuteAndLogOutput;
                        }
                        else
                        {
                            executeAction = ExecuteNonQuery;
                        }
                        // Execute within a wrapper that allows a provider specific derived class to handle provider specific exception.
                        ExecuteCommandsWithinExceptionHandler(index, script, () =>
                        {
                            executeAction(command);
                        });
                    }

                    if (UseTheSameTransactionForJournalTableAndScripts)
                    {
                        journal.StoreExecutedScript(script, dbCommandFactory);
                    }
                });
                if (!UseTheSameTransactionForJournalTableAndScripts)
                {
                    connectionManager.ExecuteCommandsWithManagedConnection(dbCommandFactory =>
                    {
                        var journal = journalFactory();
                        journal.StoreExecutedScript(script, dbCommandFactory);
                    });
                }
            }
            catch (DbException sqlException)
            {
                Log().WriteInformation("DB exception has occured in script: {0}'", script.Name);
                Log().WriteError("Script block number: {0}; Message: {1}", index, sqlException.Message);
                Log().WriteError("{0}", sqlException.ToString());
                throw;
            }
            catch (Exception ex)
            {
                Log().WriteInformation("Exception has occured in script: {0}", script.Name);
                Log().WriteError("{0}", ex.ToString());
                throw;
            }
        }

        protected abstract void ExecuteCommandsWithinExceptionHandler(int index, SqlScript script, Action executeCallback);

        protected virtual void ExecuteNonQuery(IDbCommand command)
        {
            command.ExecuteNonQuery();
        }

        protected virtual void ExecuteAndLogOutput(IDbCommand command)
        {
            using var reader = command.ExecuteReader();
            WriteReaderToLog(reader);
        }

        /// <summary>
        /// Quotes the sql object.
        /// </summary>
        /// <param name="schema"></param>
        protected string QuoteSqlObjectName(string objectName)
        {
            return sqlObjectParser.QuoteIdentifier(objectName);
        }

        protected virtual void WriteReaderToLog(IDataReader reader)
        {
            do
            {
                if (reader.FieldCount == 0)
                {
                    if (reader.RecordsAffected >= 0)
                        Log().WriteInformation("RecordsAffected: {0}", reader.RecordsAffected);
                    return;
                }

                var names = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    names.Add(reader.GetName(i));
                }

                var lines = new List<List<string>>();
                while (reader.Read())
                {
                    var line = new List<string>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        value = value == DBNull.Value ? null : value.ToString();
                        line.Add((string)value);
                    }
                    lines.Add(line);
                }

                var format = new StringBuilder();
                var totalLength = 0;
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var maxLength = (lines.Count == 0 ? 0 : lines.Max(l => (l[i] ?? "").Length)) + 2;
                    format.Append($" {{{i}, {maxLength}}} |");
                    totalLength += (maxLength + 3);
                }
                format.Append($"|{format}");
                totalLength += 1;

                var delimiterLine = new string('-', totalLength);
                Log().WriteInformation(delimiterLine);
                Log().WriteInformation(string.Format(format.ToString(), names.ToArray()));
                Log().WriteInformation(delimiterLine);
                foreach (var line in lines)
                {
                    Log().WriteInformation(string.Format(format.ToString(), line.ToArray()));
                }
                Log().WriteInformation(delimiterLine);
                Log().WriteInformation("\r\n");
            } while (reader.NextResult());
        }

        protected Func<IUpgradeLog> Log { get; private set; }
    }
}