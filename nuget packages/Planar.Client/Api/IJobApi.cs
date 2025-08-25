using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    /// <summary>
    /// Represents a collection of functions to interact with the Job API endpoints
    /// </summary>
    public interface IJobApi
    {
        /// <summary>
        /// Delete Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="key">Data key</param>
        /// <returns></returns>
        Task DeleteDataAsync(string id, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear All Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        Task ClearDataAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Job Settings
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>IEnumerable&lt;KeyValueItem&gt;</returns>
        Task<IEnumerable<KeyValueItem>> GatSettingsAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// List Jobs With Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>IPagingResponse&lt;JobRowDetails&gt;</returns>

#if NETSTANDARD2_0

        Task<IPagingResponse<JobBasicDetails>> ListAsync(ListJobsFilter filter = null, CancellationToken cancellationToken = default);

#else
        Task<IPagingResponse<JobBasicDetails>> ListAsync(ListJobsFilter? filter = null, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Get Audit
        /// </summary>
        /// <param name="auditId"></param>
        /// <returns>JobAuditWithInformation</returns>
        Task<JobAuditWithInformation> GetAuditAsync(int auditId, CancellationToken cancellationToken = default);

        /// <summary>
        /// List All Jobs Audits
        /// </summary>
        /// <param name="paging"></param>
        /// <returns>IPagingResponse&lt;JobAudit&gt</returns>
        Task<IPagingResponse<JobAudit>> ListAllAuditsAsync(
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Job Detaild
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>JobDetails</returns>
        Task<JobDetails> GetAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// List Audits For Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>IPagingResponse&lt;JobAudit&gt</returns>
        Task<IPagingResponse<JobAudit>> ListAuditsAsync(
            string id,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Job Peripheral Detials
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>JobDescription</returns>
        Task<JobDescription> DescribeJobAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get JobFile.yml Template
        /// </summary>
        /// <param name="name"></param>
        /// <returns>string</returns>
        Task<string> GetJobFileAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Next Running
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>string</returns>
        Task<DateTime?> GetNextRunningAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Previous Running
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>string</returns>
        Task<DateTime?> GetPreviousRunningAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gat All Running Jobs
        /// </summary>
        /// <returns>List&lt;RunningJobDetails&gt;</returns>
        Task<IEnumerable<RunningJobDetails>> GetRunningAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Runnig Job Info
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns>RunningJobDetails</returns>
        Task<RunningJobDetails> GetRunningAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Running Job Data
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns>RunningJobData</returns>
        Task<RunningJobData> GetRunningDataAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get All Job Types Get all job types
        /// </summary>
        /// <returns>List&lt;string&gt;</returns>
        Task<IEnumerable<string>> GetJobTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel Running Job
        /// </summary>
        /// <param name="fireInstanceId">Fire instance id</param>
        /// <returns></returns>
        Task CancelRunningAsync(string fireInstanceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add Job
        /// </summary>
        /// <returns>string</returns>
#if NETSTANDARD2_0

        Task<string> AddAsync(string folder, string jobFileName, CancellationToken cancellationToken = default);

#else
        Task<string> AddAsync(string folder, string? jobFileName, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Invoke Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="nowOverrideValue"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

#if NETSTANDARD2_0

        Task InvokeAsync(
            string id,
            TimeSpan? timeout = null,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string> data = null,
            int? maxRetries = null,
            TimeSpan? retrySpan = null,
            CancellationToken cancellationToken = default);

#else
        Task InvokeAsync(
            string id,
            TimeSpan? timeout = null,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string?>? data = null,
            int? maxRetries = null,
            TimeSpan? retrySpan = null,
            CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Invoke Job, track progress and wait for completion
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        /// <param name="nowOverrideValue"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
#if NETSTANDARD2_0

        Task TestAsync(
            string id,
            Func<RunningJobDetails, Task> callback,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string> data = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

#else
        Task TestAsync(
            string id,
            Func<RunningJobDetails, Task> callback,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string?>? data = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Pause Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="autoResumeDate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PauseAsync(string id, DateTime? autoResumeDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pause Group Of Job
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PauseGroupAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume Group Of Job
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ResumeGroupAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set Auto Resume To Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="autoResumeDate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetAutoResumeAsync(string id, DateTime autoResumeDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel Auto Resume From Job
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CancelAutoResumeAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queue Invokation Of Job Queue invokation of job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="dueDate">Invocation due date</param>
        /// <param name="timeout">Specific timeout</param>
        /// <param name="nowOverrideValue"></param>
        /// <param name="data">Key/Value dictionary</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
#if NETSTANDARD2_0

        Task<string> QueueInvokeAsync(
            string id,
            DateTime dueDate,
            TimeSpan? timeout,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string> data = null,
            int? maxRetries = null,
            TimeSpan? retrySpan = null,
            CancellationToken cancellationToken = default);

#else
        Task<string> QueueInvokeAsync(
            string id,
            DateTime dueDate,
            TimeSpan? timeout,
            DateTime? nowOverrideValue = null,
            Dictionary<string, string?>? data = null,
            int? maxRetries = null,
            TimeSpan? retrySpan = null,
            CancellationToken cancellationToken = default);
#endif

#if NETSTANDARD2_0

        Task WaitAsync(string id, string group, CancellationToken cancellationToken = default);

#else
        Task WaitAsync(string? id, string? group, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Set auto resume date for job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="autoResumeDate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ResumeAsync(string id, DateTime? autoResumeDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add Or Update Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="key">Data key</param>
        /// <param name="key">Data value</param>
        /// <returns></returns>
        Task PutDataAsync(string id, string key, string value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update Job
        /// </summary>
        /// <returns>string</returns>
        Task<string> UpdateAsync(
            string id,
            bool updateJobData = false,
            bool updateTriggersData = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="author">Author name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetAuthor(string id, string author, CancellationToken cancellationToken = default);
    }
}