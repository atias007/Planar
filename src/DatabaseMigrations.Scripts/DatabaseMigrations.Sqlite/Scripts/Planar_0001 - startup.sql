DROP TABLE IF EXISTS QRTZ_FIRED_TRIGGERS;
DROP TABLE IF EXISTS QRTZ_PAUSED_TRIGGER_GRPS;
DROP TABLE IF EXISTS QRTZ_SCHEDULER_STATE;
DROP TABLE IF EXISTS QRTZ_LOCKS;
DROP TABLE IF EXISTS QRTZ_SIMPROP_TRIGGERS;
DROP TABLE IF EXISTS QRTZ_SIMPLE_TRIGGERS;
DROP TABLE IF EXISTS QRTZ_CRON_TRIGGERS;
DROP TABLE IF EXISTS QRTZ_BLOB_TRIGGERS;
DROP TABLE IF EXISTS QRTZ_TRIGGERS;
DROP TABLE IF EXISTS QRTZ_JOB_DETAILS;
DROP TABLE IF EXISTS QRTZ_CALENDARS;

CREATE TABLE QRTZ_JOB_DETAILS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	JOB_NAME NVARCHAR(150) NOT NULL,
    JOB_GROUP NVARCHAR(150) NOT NULL,
    DESCRIPTION NVARCHAR(250) NULL,
    JOB_CLASS_NAME   NVARCHAR(250) NOT NULL,
    IS_DURABLE BIT NOT NULL,
    IS_NONCONCURRENT BIT NOT NULL,
    IS_UPDATE_DATA BIT  NOT NULL,
	REQUESTS_RECOVERY BIT NOT NULL,
    JOB_DATA BLOB NULL,
    PRIMARY KEY (SCHED_NAME,JOB_NAME,JOB_GROUP)
);

CREATE TABLE QRTZ_TRIGGERS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	TRIGGER_NAME NVARCHAR(150) NOT NULL,
    TRIGGER_GROUP NVARCHAR(150) NOT NULL,
    JOB_NAME NVARCHAR(150) NOT NULL,
    JOB_GROUP NVARCHAR(150) NOT NULL,
    DESCRIPTION NVARCHAR(250) NULL,
    NEXT_FIRE_TIME BIGINT NULL,
    PREV_FIRE_TIME BIGINT NULL,
    PRIORITY INTEGER NULL,
    TRIGGER_STATE NVARCHAR(16) NOT NULL,
    TRIGGER_TYPE NVARCHAR(8) NOT NULL,
    START_TIME BIGINT NOT NULL,
    END_TIME BIGINT NULL,
    CALENDAR_NAME NVARCHAR(200) NULL,
    MISFIRE_INSTR INTEGER NULL,
    JOB_DATA BLOB NULL,
    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
    FOREIGN KEY (SCHED_NAME,JOB_NAME,JOB_GROUP)
        REFERENCES QRTZ_JOB_DETAILS(SCHED_NAME,JOB_NAME,JOB_GROUP)
);

CREATE TABLE QRTZ_SIMPLE_TRIGGERS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	TRIGGER_NAME NVARCHAR(150) NOT NULL,
    TRIGGER_GROUP NVARCHAR(150) NOT NULL,
    REPEAT_COUNT BIGINT NOT NULL,
    REPEAT_INTERVAL BIGINT NOT NULL,
    TIMES_TRIGGERED BIGINT NOT NULL,
    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
        REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP) ON DELETE CASCADE
);

CREATE TRIGGER DELETE_SIMPLE_TRIGGER DELETE ON QRTZ_TRIGGERS
BEGIN
	DELETE FROM QRTZ_SIMPLE_TRIGGERS WHERE SCHED_NAME=OLD.SCHED_NAME AND TRIGGER_NAME=OLD.TRIGGER_NAME AND TRIGGER_GROUP=OLD.TRIGGER_GROUP;
END
;

CREATE TABLE QRTZ_SIMPROP_TRIGGERS 
  (
    SCHED_NAME NVARCHAR (120) NOT NULL ,
    TRIGGER_NAME NVARCHAR (150) NOT NULL ,
    TRIGGER_GROUP NVARCHAR (150) NOT NULL ,
    STR_PROP_1 NVARCHAR (512) NULL,
    STR_PROP_2 NVARCHAR (512) NULL,
    STR_PROP_3 NVARCHAR (512) NULL,
    INT_PROP_1 INT NULL,
    INT_PROP_2 INT NULL,
    LONG_PROP_1 BIGINT NULL,
    LONG_PROP_2 BIGINT NULL,
    DEC_PROP_1 NUMERIC NULL,
    DEC_PROP_2 NUMERIC NULL,
    BOOL_PROP_1 BIT NULL,
    BOOL_PROP_2 BIT NULL,
    TIME_ZONE_ID NVARCHAR(80) NULL,
	PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
	FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
        REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP) ON DELETE CASCADE
);

CREATE TRIGGER DELETE_SIMPROP_TRIGGER DELETE ON QRTZ_TRIGGERS
BEGIN
	DELETE FROM QRTZ_SIMPROP_TRIGGERS WHERE SCHED_NAME=OLD.SCHED_NAME AND TRIGGER_NAME=OLD.TRIGGER_NAME AND TRIGGER_GROUP=OLD.TRIGGER_GROUP;
END
;

CREATE TABLE QRTZ_CRON_TRIGGERS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	TRIGGER_NAME NVARCHAR(150) NOT NULL,
    TRIGGER_GROUP NVARCHAR(150) NOT NULL,
    CRON_EXPRESSION NVARCHAR(250) NOT NULL,
    TIME_ZONE_ID NVARCHAR(80),
    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
        REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP) ON DELETE CASCADE
);

CREATE TRIGGER DELETE_CRON_TRIGGER DELETE ON QRTZ_TRIGGERS
BEGIN
	DELETE FROM QRTZ_CRON_TRIGGERS WHERE SCHED_NAME=OLD.SCHED_NAME AND TRIGGER_NAME=OLD.TRIGGER_NAME AND TRIGGER_GROUP=OLD.TRIGGER_GROUP;
END
;

CREATE TABLE QRTZ_BLOB_TRIGGERS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	TRIGGER_NAME NVARCHAR(150) NOT NULL,
    TRIGGER_GROUP NVARCHAR(150) NOT NULL,
    BLOB_DATA BLOB NULL,
    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
        REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP) ON DELETE CASCADE
);

CREATE TRIGGER DELETE_BLOB_TRIGGER DELETE ON QRTZ_TRIGGERS
BEGIN
	DELETE FROM QRTZ_BLOB_TRIGGERS WHERE SCHED_NAME=OLD.SCHED_NAME AND TRIGGER_NAME=OLD.TRIGGER_NAME AND TRIGGER_GROUP=OLD.TRIGGER_GROUP;
END
;

CREATE TABLE QRTZ_CALENDARS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	CALENDAR_NAME  NVARCHAR(200) NOT NULL,
    CALENDAR BLOB NOT NULL,
    PRIMARY KEY (SCHED_NAME,CALENDAR_NAME)
);

CREATE TABLE QRTZ_PAUSED_TRIGGER_GRPS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	TRIGGER_GROUP NVARCHAR(150) NOT NULL, 
    PRIMARY KEY (SCHED_NAME,TRIGGER_GROUP)
);

CREATE TABLE QRTZ_FIRED_TRIGGERS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	ENTRY_ID NVARCHAR(140) NOT NULL,
    TRIGGER_NAME NVARCHAR(150) NOT NULL,
    TRIGGER_GROUP NVARCHAR(150) NOT NULL,
    INSTANCE_NAME NVARCHAR(200) NOT NULL,
    FIRED_TIME BIGINT NOT NULL,
    SCHED_TIME BIGINT NOT NULL,
	PRIORITY INTEGER NOT NULL,
    STATE NVARCHAR(16) NOT NULL,
    JOB_NAME NVARCHAR(150) NULL,
    JOB_GROUP NVARCHAR(150) NULL,
    IS_NONCONCURRENT BIT NULL,
    REQUESTS_RECOVERY BIT NULL,
    PRIMARY KEY (SCHED_NAME,ENTRY_ID)
);

CREATE TABLE QRTZ_SCHEDULER_STATE
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	INSTANCE_NAME NVARCHAR(200) NOT NULL,
    LAST_CHECKIN_TIME BIGINT NOT NULL,
    CHECKIN_INTERVAL BIGINT NOT NULL,
    PRIMARY KEY (SCHED_NAME,INSTANCE_NAME)
);

CREATE TABLE QRTZ_LOCKS
  (
    SCHED_NAME NVARCHAR(120) NOT NULL,
	LOCK_NAME  NVARCHAR(40) NOT NULL, 
    PRIMARY KEY (SCHED_NAME,LOCK_NAME)
);

CREATE TABLE [ClusterNodes](
	[Server] [nvarchar](100) NOT NULL,
	[Port] [int] NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[ClusterPort] [int] NOT NULL,
	[JoinDate] [datetime] NOT NULL,
	[HealthCheckDate] [datetime] NOT NULL,
	[MaxConcurrency] [int] NOT NULL,
 CONSTRAINT [PK_ClusterNodes] PRIMARY KEY  
(
	[Server] ASC,
	[Port] ASC
),
 CONSTRAINT [IX_ClusterNodes] UNIQUE  
(
	[InstanceId] ASC
));


CREATE TABLE [GlobalConfig](
	[Key] [nvarchar](50) NOT NULL,
	[Value] [nvarchar](4000) NULL,
	[Type] [varchar](10) NOT NULL,
 CONSTRAINT [PK_GlobalConfig] PRIMARY KEY  
(
	[Key] ASC
));
CREATE TABLE [Groups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[AdditionalField1] [nvarchar](500) NULL,
	[AdditionalField2] [nvarchar](500) NULL,
	[AdditionalField3] [nvarchar](500) NULL,
	[AdditionalField4] [nvarchar](500) NULL,
	[AdditionalField5] [nvarchar](500) NULL,
	[Role] [varchar](20) NOT NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [JobAudit](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobId] [varchar](20) NOT NULL,
	[JobKey] [varchar](101) NOT NULL,
	[DateCreated] [datetime] NOT NULL,
	[Username] [varchar](50) NOT NULL,
	[UserTitle] [nvarchar](101) NOT NULL,
	[Description] [varchar](200) NOT NULL,
	[AdditionalInfo] [nvarchar](4000) NULL,
 CONSTRAINT [PK_JobAudit] PRIMARY KEY 
(
	[Id] ASC
));

CREATE TABLE [JobInstanceLog](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[InstanceId] [varchar](250) NOT NULL,
	[JobId] [varchar](20) NOT NULL,
	[JobName] [varchar](50) NOT NULL,
	[JobGroup] [varchar](50) NOT NULL,
	[JobType] [varchar](50) NOT NULL,
	[TriggerId] [varchar](20) NOT NULL,
	[TriggerName] [varchar](50) NOT NULL,
	[TriggerGroup] [varchar](50) NOT NULL,
	[ServerName] [nvarchar](50) NULL,
	[Status] [int] NOT NULL,
	[StatusTitle] [varchar](10) NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NULL,
	[Duration] [int] NULL,
	[EffectedRows] [int] NULL,
	[Data] [nvarchar](4000) NULL,
	[Log] [nvarchar] NULL,
	[Exception] [nvarchar] NULL,
	[ExceptionCount] [int] NOT NULL DEFAULT 0,
	[Retry] [bit] NOT NULL,
	[IsCanceled] [bit] NOT NULL DEFAULT 0,
	[Anomaly] [tinyint] NULL,
	[HasWarnings] [bit] NOT NULL DEFAULT 0,
 CONSTRAINT [PK_JobInstanceLog] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [JobProperties](
	[JobId] [varchar](20) NOT NULL,
	[Properties] [nvarchar] NULL,
 CONSTRAINT [PK_JobProperties] PRIMARY KEY  
(
	[JobId] ASC
));

CREATE TABLE [MonitorActions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](50) NOT NULL,
	[EventId] [int] NOT NULL,
	[EventArgument] [varchar](50) NULL,
	[JobName] [varchar](50) NULL,
	[JobGroup] [varchar](50) NULL,
	[GroupId] [int] NOT NULL,
	[Hook] [varchar](50) NOT NULL,
	[Active] [bit] NOT NULL DEFAULT 1,
    FOREIGN KEY(GroupId) REFERENCES Groups(Id),
 CONSTRAINT [PK_Monitor] PRIMARY KEY 
(
	[Id] ASC
));

CREATE TABLE [MonitorAlerts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MonitorId] [int] NOT NULL,
	[MonitorTitle] [nvarchar](50) NOT NULL,
	[EventId] [int] NOT NULL,
	[EventTitle] [varchar](100) NULL,
	[EventArgument] [varchar](50) NULL,
	[JobName] [varchar](50) NULL,
	[JobGroup] [varchar](50) NULL,
	[JobId] [varchar](20) NULL,
	[GroupId] [int] NOT NULL,
	[GroupName] [nvarchar](50) NOT NULL,
	[UsersCount] [int] NOT NULL,
	[Hook] [varchar](50) NOT NULL,
	[LogInstanceId] [varchar](250) NULL,
	[HasError] [bit] NOT NULL,
	[AlertDate] [datetime] NOT NULL,
	[Exception] [nvarchar] NULL,
	[AlertPayload] [nvarchar] NULL,
 CONSTRAINT [PK_MonitorAlerts] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [MonitorCounters](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MonitorId] [int] NOT NULL,
	[JobId] [varchar](20) NOT NULL,
	[Counter] [int] NOT NULL,
	[LastUpdate] [datetime] NULL,
 CONSTRAINT [PK_MonitorCounter] PRIMARY KEY 
(
	[Id] ASC
));

CREATE TABLE [MonitorHooks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](2000) NOT NULL,
	[Path] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_MonitorHooks] PRIMARY KEY 
(
	[Id] ASC
));

CREATE TABLE [MonitorMute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobId] [varchar](20) NULL,
	[MonitorId] [int] NULL,
	[DueDate] [datetime] NULL,
 CONSTRAINT [PK_MonitorMute] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [SecurityAudits](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](500) NOT NULL,
	[Username] [varchar](50) NOT NULL,
	[UserTitle] [nvarchar](101) NOT NULL,
	[DateCreated] [datetime] NOT NULL,
	[IsWarning] [bit] NOT NULL,
 CONSTRAINT [PK_SecurityAudits] PRIMARY KEY 
(
	[Id] ASC
));

CREATE TABLE [Trace](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar] NULL,
	[Level] [nvarchar](128) NULL,
	[TimeStamp] [datetimeoffset](7) NOT NULL,
	[Exception] [nvarchar] NULL,
	[LogEvent] [nvarchar] NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY 
(
	[Id] ASC
));

CREATE TABLE [Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [varchar](50) NOT NULL,
	[Password] [varbinary](128) NOT NULL,
	[Salt] [varbinary](128) NOT NULL,
	[RoleId] [int] NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NULL,
	[EmailAddress1] [nvarchar](250) NULL,
	[EmailAddress2] [nvarchar](250) NULL,
	[EmailAddress3] [nvarchar](250) NULL,
	[PhoneNumber1] [nvarchar](50) NULL,
	[PhoneNumber2] [nvarchar](50) NULL,
	[PhoneNumber3] [nvarchar](50) NULL,
	[AdditionalField1] [nvarchar](500) NULL,
	[AdditionalField2] [nvarchar](500) NULL,
	[AdditionalField3] [nvarchar](500) NULL,
	[AdditionalField4] [nvarchar](500) NULL,
	[AdditionalField5] [nvarchar](500) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY  
(
	[Id] ASC
),
 CONSTRAINT [IX_Users] UNIQUE  
(
	[Username] ASC
));

CREATE TABLE [UsersToGroups](
	[UserId] [int] NOT NULL,
	[GroupId] [int] NOT NULL,
    FOREIGN KEY(UserId) REFERENCES Users(Id),
    FOREIGN KEY(GroupId) REFERENCES Groups(Id),
 CONSTRAINT [PK_UsersToGroups] PRIMARY KEY  
(
	[UserId] ASC,
	[GroupId] ASC
));

CREATE TABLE [Statistics_ConcurrentExecution](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [datetime] NOT NULL,
	[Server] [nvarchar](100) NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[MaxConcurrent] [int] NOT NULL,
 CONSTRAINT [PK_ConcurentExecution] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [Statistics_ConcurrentQueue](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [datetime] NOT NULL,
	[Server] [nvarchar](100) NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[ConcurrentValue] [int] NOT NULL,
 CONSTRAINT [PK_ConcurentQueue] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [Statistics_JobCounters](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobId] [varchar](20) NOT NULL,
	[RunDate] [date] NOT NULL,
	[TotalRuns] [int] NOT NULL,
	[SuccessRetries] [int] NULL,
	[FailRetries] [int] NULL,
	[Recovers] [int] NULL,
 CONSTRAINT [PK_JobCounters] PRIMARY KEY  
(
	[Id] ASC
));

CREATE TABLE [Statistics_JobDurationStatistics](
	[JobId] [varchar](20) NOT NULL,
	[AvgDuration] [numeric](18, 4) NOT NULL,
	[StdevDuration] [numeric](18, 4) NOT NULL,
	[Rows] [int] NOT NULL,
 CONSTRAINT [PK_JobDurationStatistics] PRIMARY KEY  
(
	[JobId] ASC
));

CREATE TABLE [Statistics_JobEffectedRowsStatistics](
	[JobId] [varchar](20) NOT NULL,
	[AvgEffectedRows] [numeric](18, 4) NOT NULL,
	[StdevEffectedRows] [numeric](18, 4) NOT NULL,
	[Rows] [int] NOT NULL,
 CONSTRAINT [PK_JobEffectedRowsStatistics] PRIMARY KEY  
(
	[JobId] ASC
));

CREATE UNIQUE INDEX [IX_Groups] ON [Groups]
(
	[Name] ASC
);

CREATE INDEX [IX_JobInstanceLog] ON [JobInstanceLog]
(
	[HasWarnings] ASC
);

CREATE INDEX [IX_JobInstanceLog_InstanceId] ON [JobInstanceLog]
(
	[InstanceId] ASC,
	[StartDate] ASC
);

CREATE INDEX [IX_JobInstanceLog_JobGroup] ON [JobInstanceLog]
(
	[JobGroup] ASC,
	[StartDate] ASC
);

CREATE INDEX [IX_JobInstanceLog_JobId] ON [JobInstanceLog]
(
	[JobId] ASC,
	[StartDate] ASC
);

CREATE INDEX [IX_JobInstanceLog_JobType] ON [JobInstanceLog]
(
	[JobType] ASC,
	[StartDate] ASC
);

CREATE INDEX [IX_JobInstanceLog_StartDate] ON [JobInstanceLog]
(
	[StartDate] ASC
);

CREATE INDEX [IX_JobInstanceLog_StartGroup] ON [JobInstanceLog]
(
	[StartDate] ASC,
	[JobGroup] ASC
);

CREATE INDEX [IX_JobInstanceLog_StartGroupType] ON [JobInstanceLog]
(
	[StartDate] ASC,
	[JobGroup] ASC,
	[JobType] ASC
);

CREATE INDEX [IX_JobInstanceLog_StartType] ON [JobInstanceLog]
(
	[StartDate] ASC,
	[JobType] ASC
);

CREATE INDEX [IX_JobInstanceLog_Status] ON [JobInstanceLog]
(
	[Status] ASC,
	[StartDate] ASC
);

CREATE UNIQUE INDEX [IX_MonitorCounters] ON [MonitorCounters]
(
	[JobId] ASC,
	[MonitorId] ASC
);

CREATE UNIQUE INDEX [IX_MonitorHooks] ON [MonitorHooks]
(
	[Name] ASC
);

CREATE UNIQUE INDEX [IX_MonitorMute] ON [MonitorMute]
(
	[JobId] ASC,
	[MonitorId] ASC
);

CREATE UNIQUE INDEX [IX_JobCounters] ON [Statistics_JobCounters]
(
	[RunDate] ASC,
	[JobId] ASC
);
