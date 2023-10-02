using Planar.Client.Entities;
using System;
using System.Collections.Generic;

namespace Planar.Client
{
    internal class JobApi : IJobApi
    {
        private readonly RestProxy _proxy;

        public JobApi(RestProxy proxy)
        {
            _proxy = proxy;
        }

        public string Add(string folder)
        {
            throw new NotImplementedException();
        }

        public string Add(string folder, string jobFileName)
        {
            throw new NotImplementedException();
        }

        public void AddData(string id, string key, string value)
        {
            throw new NotImplementedException();
        }

        public void CancelRunning(string fireInstanceId)
        {
            throw new NotImplementedException();
        }

        public void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteData(string id, string key)
        {
            throw new NotImplementedException();
        }

        public JobDescription DescribeJob(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValueItem> GatSettings(string id)
        {
            throw new NotImplementedException();
        }

        public IPagingResponse<JobAudit> GetAllAudits(PagingRequest? paging)
        {
            throw new NotImplementedException();
        }

        public JobAuditWithInformation GetAudit(int auditId)
        {
            throw new NotImplementedException();
        }

        public IPagingResponse<JobAudit> GetAudits(string id, PagingRequest? paging)
        {
            throw new NotImplementedException();
        }

        public JobDetails GetJob(string id)
        {
            throw new NotImplementedException();
        }

        public string GetJobFile(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetJobTypes()
        {
            throw new NotImplementedException();
        }

        public DateTime? GetNextRunning(string id)
        {
            throw new NotImplementedException();
        }

        public DateTime? GetPreviousRunning(string id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RunningJobDetails> GetRunning()
        {
            throw new NotImplementedException();
        }

        public RunningJobData GetRunningData(string instanceId)
        {
            throw new NotImplementedException();
        }

        public RunningJobDetails GetRunningInfo(string instanceId)
        {
            throw new NotImplementedException();
        }

        public void Invoke(string id)
        {
            throw new NotImplementedException();
        }

        public void Invoke(string id, DateTime nowOverrideValue)
        {
            throw new NotImplementedException();
        }

        public IPagingResponse<JobBasicDetails> List(JobsFilter filter)
        {
            throw new NotImplementedException();
        }

        public void Pause(string id)
        {
            throw new NotImplementedException();
        }

        public void PauseAll()
        {
            throw new NotImplementedException();
        }

        public void QueueInvoke(string id, DateTime dueDate)
        {
            throw new NotImplementedException();
        }

        public void QueueInvoke(string id, DateTime dueDate, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Resume(string id)
        {
            throw new NotImplementedException();
        }

        public void ResumeAll()
        {
            throw new NotImplementedException();
        }

        public string Update(string folder, bool updateJobData = true, bool updateTriggerData = true)
        {
            throw new NotImplementedException();
        }

        public string Update(string folder, string jobFileName, bool updateJobData = true, bool updateTriggerData = true)
        {
            throw new NotImplementedException();
        }

        public void UpdateJobData(string id, string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}