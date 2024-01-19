using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
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
        Task<IPagingResponse<JobBasicDetails>> ListAsync(JobsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Audit
        /// </summary>
        /// <param name="auditId"></param>
        /// <returns>JobAuditWithInformation</returns>
        Task<JobAuditWithInformation> GetAuditAsync(int auditId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get All Jobs Audits
        /// </summary>
        /// <param name="paging"></param>
        /// <returns>IPagingResponse&lt;JobAudit&gt</returns>
        Task<IPagingResponse<JobAudit>> GetAllAuditsAsync(PagingRequest? paging, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Job Detaild
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>JobDetails</returns>
        Task<JobDetails> GetAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Audits For Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="paging"></param>
        /// <returns>IPagingResponse&lt;JobAudit&gt</returns>
        Task<IPagingResponse<JobAudit>> GetAuditsAsync(string id, PagingRequest? paging, CancellationToken cancellationToken = default);

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
        Task<RunningJobDetails> GetRunningInfoAsync(string instanceId, CancellationToken cancellationToken = default);

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
        Task<string> AddAsync(string folder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add Job
        /// </summary>
        /// <returns>string</returns>
        Task<string> AddAsync(string folder, string jobFileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoke Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        Task InvokeAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoke Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="nowOverrideValue"></param>
        /// <returns></returns>
        Task InvokeAsync(string id, DateTime nowOverrideValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pause Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        Task PauseAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queue Invokation Of Job Queue invokation of job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="dueDate">Invocation due date</param>
        /// <returns></returns>
        Task QueueInvokeAsync(string id, DateTime dueDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queue Invokation Of Job Queue invokation of job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="dueDate">Invocation due date</param>
        /// <param name="timeout">specific timeout</param>
        /// <returns></returns>
        Task QueueInvokeAsync(string id, DateTime dueDate, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        Task ResumeAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="key">Data key</param>
        /// <param name="key">Data value</param>
        /// <returns></returns>
        Task AddDataAsync(string id, string key, string value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        Task UpdateJobDataAsync(string id, string key, string value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update Job
        /// </summary>
        /// <returns>string</returns>
        Task<string> UpdateAsync(string folder, bool updateJobData = true, bool updateTriggerData = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update Job
        /// </summary>
        /// <returns>string</returns>
        Task<string> UpdateAsync(string folder, string jobFileName, bool updateJobData = true, bool updateTriggerData = true, CancellationToken cancellationToken = default);
    }
}