using System;
using System.Text.RegularExpressions;

namespace Planar
{
    internal static class Consts
    {
        public const string CryptographyKeyVariableKey = "PLANAR_CRYPTOGRAPHY_KEY";
        public const string PlanarJobArgumentContextFolder = "context";

        // ********** DONT FIX, COMMON FILE TO STANDART 2.0 PROJECT ********** //
        public static readonly string[] PreserveGroupNames = new string[] { RetryTriggerGroup, PlanarSystemGroup };

        // ********** DONT FIX, COMMON FILE TO STANDART 2.0 PROJECT ********** //
        public static readonly string[] AllDataKeys = new string[] {
            RetryCounter,
            RetrySpan,
            MaxRetries,
            RetryTriggerGroup,
            QueueInvokeTriggerGroup,
            RetryTriggerNamePrefix,
            PlanarSystemGroup,
            JobId,
            TriggerId,
            TriggerTimeout,
            NowOverrideValue,
            Author,
            CircuitBreaker,
            LogRetentionDays
        };

        public const int CliDefaultPageSize = 25;

        public const int MaximumJobDataItems = 1000;
        public const string Undefined = "undefined";
        public const string Unauthorized = "unauthorized";

        public const string QuartzPrefix = "QRTZ_";
        public const string ConstPrefix = "__";
        public const string RetryCounter = "__Job_Retry_Counter";
        public const string RetrySpan = "__Job_Retry_Span";
        public const string MaxRetries = "__Job_Retry_Max";
        public const string WorkflowInstanceIdDataKey = "__Workflow_Instance_Id";
        public const string WorkflowTriggerIdDataKey = "__Workflow_Trigger_Id";
        public const int DefaultMaxRetries = 3;

        public const string QueueInvokeTriggerGroup = "__QueueInvoke";
        public const string RetryTriggerGroup = "__RetryTrigger";
        public const string RetryTriggerNamePrefix = "Retry.Count";
        public const string PlanarSystemGroup = "__System";

        public const string JobId = "__Job_Id";
        public const string Author = "__Author";
        public const string LogRetentionDays = "__LogRetentionDays";
        public const string TriggerId = "__Trigger_Id";
        public const string TriggerTimeout = "__Trigger_Timeout";
        public const string NowOverrideValue = "__Now_Override_Value";

        public const string CircuitBreaker = "__CircuitBreaker";
        public const string CircuitBreakerTriggerGroup = "__CircuitBreakerResume";

        public const string ManualTriggerId = "Manual";
        public const string WorkflowTriggerId = "Workflow";
        public const string LogLevelSettingsKey1 = "Log Level";
        public const string LogLevelSettingsKey2 = "LogLevel";

        public const int MaxConcurrencyDefaultValue = 10;
        public static readonly TimeSpan PersistRunningJobsSpanDefaultValue = TimeSpan.FromMinutes(5);
        public const string ProductionEnvironment = "Production";
        public const string RecoveringJobsGroup = "RECOVERING_JOBS";

        public const string HookNewLineLogText = "~~~<newline>~~~";

        public const string CliMessageHeaderName = "planar-cli-message";
        public const string CliSuggestionHeaderName = "planar-cli-suggestion";
        public const string CliUserAgent = "Planar.CLI.V";
        public const string CliUserName = "Planar-CLI-UserName";
        public const string CliUserDomainName = "Planar-CLI-UserDomainName";

        // ********** DONT FIX, COMMON FILE TO STANDART 2.0 PROJECT ********** //
        public static readonly Regex EmailRegex = new Regex(
            @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9]{2,8}(?:[a-z0-9-]*[a-z0-9])?)\Z",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(3));

        public static bool IsDataKeyValid(string key)
        {
            return !Array.Exists(AllDataKeys, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}