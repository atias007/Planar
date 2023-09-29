using System;

namespace Planar;

internal class OldAppSettings
{
    public string Environment { get; set; }
    public string DatabaseProvider { get; set; }
    public string ServiceName { get; set; }
    public string InstanceId { get; set; }
    public string DatabaseConnectionString { get; set; }
    public int MaxConcurrency { get; set; }
    public bool Clustering { get; set; }
    public short ClusterPort { get; set; }
    public TimeSpan JobAutoStopSpan { get; set; }
    public TimeSpan ClusteringCheckinInterval { get; set; }
    public TimeSpan ClusteringCheckinMisfireThreshold { get; set; }
    public TimeSpan ClusterHealthCheckInterval { get; set; }
    public TimeSpan PersistRunningJobsSpan { get; set; }
    public TimeSpan SchedulerStartupDelay { get; set; }
    public short HttpPort { get; set; }
    public bool UseHttps { get; set; }
    public short HttpsPort { get; set; }
    public bool UseHttpsRedirect { get; set; }
    public string LogLevel { get; set; }
    public bool SwaggerUI { get; set; }
    public bool OpenApiUI { get; set; }
    public bool DeveloperExceptionPage { get; set; }
    public int ClearTraceTableOverDays { get; set; }
    public int ClearJobLogTableOverDays { get; set; }
    public int ClearStatisticsTablesOverDays { get; set; }
    public bool RunDatabaseMigration { get; set; }

    public string AuthenticationMode { get; set; } = "all anonymous";

    private string _authenticationSecret;

    public string AuthenticationSecret
    {
        get { return string.IsNullOrWhiteSpace(_authenticationSecret) ? "ecawiasqrpqrgyhwnolrudpbsrwaynbqdayndnmcehjnwqyouikpodzaqxivwkconwqbhrmxfgccbxbyljguwlxhdlcvxlutbnwjlgpfhjgqbegtbxbvwnacyqnltrby" : _authenticationSecret; }
        set { _authenticationSecret = value; }
    }

    private TimeSpan _authenticationTokenExpire;

    public TimeSpan AuthenticationTokenExpire
    {
        get { return _authenticationTokenExpire == TimeSpan.Zero ? TimeSpan.FromMinutes(20) : _authenticationTokenExpire; }
        set { _authenticationTokenExpire = value; }
    }

    private int _maxConcurrency;

    public int ConcurrencyRateLimiting
    {
        get { return _maxConcurrency <= 0 ? 10 : _maxConcurrency; }
        set { _maxConcurrency = value; }
    }
}