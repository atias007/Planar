using Planar.Client.Entities;

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
        void Delete(string id);

        /// <summary>
        /// Delete Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="key">Data key</param>
        /// <returns></returns>
        void DeleteData(string id, string key);

        /// <summary>
        /// Get Job Settings
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>IEnumerable&lt;KeyValueItem&gt;</returns>
        IEnumerable<KeyValueItem> GatSettings(string id);

        /// <summary>
        /// List Jobs With Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>IPagingResponse&lt;JobRowDetails&gt;</returns>
        IPagingResponse<JobBasicDetails> List(JobsFilter filter);

        /// <summary>
        /// Get Audit
        /// </summary>
        /// <param name="auditId"></param>
        /// <returns>JobAuditWithInformation</returns>
        JobAuditWithInformation GetAudit(int auditId);

        /// <summary>
        /// Get All Jobs Audits
        /// </summary>
        /// <param name="paging"></param>
        /// <returns>IPagingResponse&lt;JobAudit&gt</returns>
        IPagingResponse<JobAudit> GetAllAudits(PagingRequest? paging);

        /// <summary>
        /// Get Job Detaild
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>JobDetails</returns>
        JobDetails GetJob(string id);

        /// <summary>
        /// Get Audits For Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="paging"></param>
        /// <returns>IPagingResponse&lt;JobAudit&gt</returns>
        IPagingResponse<JobAudit> GetAudits(string id, PagingRequest? paging);

        /// <summary>
        /// Get Job Peripheral Detials
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>JobDescription</returns>
        JobDescription DescribeJob(string id);

        /// <summary>
        /// Get JobFile.yml Template
        /// </summary>
        /// <param name="name"></param>
        /// <returns>string</returns>
        string GetJobFile(string name);

        /// <summary>
        /// Get Next Running
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>string</returns>
        DateTime? GetNextRunning(string id);

        /// <summary>
        /// Get Previous Running
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns>string</returns>
        DateTime? GetPreviousRunning(string id);

        /// <summary>
        /// Gat All Running Jobs
        /// </summary>
        /// <returns>List&lt;RunningJobDetails&gt;</returns>
        IEnumerable<RunningJobDetails> GetRunning();

        /// <summary>
        /// Get Runnig Job Info
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns>RunningJobDetails</returns>
        RunningJobDetails GetRunningInfo(string instanceId);

        /// <summary>
        /// Get Running Job Data
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns>RunningJobData</returns>
        RunningJobData GetRunningData(string instanceId);

        /// <summary>
        /// Get All Job Types Get all job types
        /// </summary>
        /// <returns>List&lt;string&gt;</returns>
        IEnumerable<string> GetJobTypes();

        /// <summary>
        /// Cancel Running Job
        /// </summary>
        /// <param name="fireInstanceId">Fire instance id</param>
        /// <returns></returns>
        void CancelRunning(string fireInstanceId);

        /// <summary>
        /// Add Job
        /// </summary>
        /// <returns>string</returns>
        string Add(string folder);

        /// <summary>
        /// Add Job
        /// </summary>
        /// <returns>string</returns>
        string Add(string folder, string jobFileName);

        /// <summary>
        /// Invoke Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        void Invoke(string id);

        /// <summary>
        /// Invoke Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="nowOverrideValue"></param>
        /// <returns></returns>
        void Invoke(string id, DateTime nowOverrideValue);

        /// <summary>
        /// Pause Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        void Pause(string id);

        /// <summary>
        /// Pause All Jobs
        /// </summary>
        /// <returns></returns>
        void PauseAll();

        /// <summary>
        /// Queue Invokation Of Job Queue invokation of job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="dueDate">Invocation due date</param>
        /// <returns></returns>
        void QueueInvoke(string id, DateTime dueDate);

        /// <summary>
        /// Queue Invokation Of Job Queue invokation of job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="dueDate">Invocation due date</param>
        /// <param name="timeout">specific timeout</param>
        /// <returns></returns>
        void QueueInvoke(string id, DateTime dueDate, TimeSpan timeout);

        /// <summary>
        /// Resume Job
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        void Resume(string id);

        /// <summary>
        /// Resume All Jobs
        /// </summary>
        /// <returns></returns>
        void ResumeAll();

        /// <summary>
        /// Add Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <param name="key">Data key</param>
        /// <param name="key">Data value</param>
        /// <returns></returns>
        void AddData(string id, string key, string value);

        /// <summary>
        /// Update Job Data
        /// </summary>
        /// <param name="id">Job id or job key (Group.Name)</param>
        /// <returns></returns>
        void UpdateJobData(string id, string key, string value);

        /// <summary>
        /// Update Job
        /// </summary>
        /// <returns>string</returns>
        string Update(string folder, bool updateJobData = true, bool updateTriggerData = true);

        /// <summary>
        /// Update Job
        /// </summary>
        /// <returns>string</returns>
        string Update(string folder, string jobFileName, bool updateJobData = true, bool updateTriggerData = true);
    }
}