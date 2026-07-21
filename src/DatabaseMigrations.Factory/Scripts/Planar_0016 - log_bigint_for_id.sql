ALTER TABLE dbo.JobInstanceLog
	DROP CONSTRAINT DF_JobInstanceLog_IsStopped
GO
CREATE TABLE dbo.Tmp_JobInstanceLog
	(
	Id bigint NOT NULL IDENTITY (1, 1),
	InstanceId varchar(250) NOT NULL,
	JobId varchar(20) NOT NULL,
	JobName varchar(50) NOT NULL,
	JobGroup varchar(50) NOT NULL,
	JobType varchar(50) NOT NULL,
	TriggerId varchar(20) NOT NULL,
	TriggerName varchar(50) NOT NULL,
	TriggerGroup varchar(50) NOT NULL,
	ServerName nvarchar(50) NULL,
	Status int NOT NULL,
	StatusTitle varchar(10) NULL,
	StartDate datetime NOT NULL,
	EndDate datetime NULL,
	Duration int NULL,
	EffectedRows int NULL,
	Data nvarchar(4000) NULL,
	[Log] nvarchar(MAX) NULL,
	Exception nvarchar(MAX) NULL,
	Retry bit NOT NULL,
	IsStopped bit NOT NULL,
	Anomaly tinyint NULL
	) 
GO

SET IDENTITY_INSERT dbo.Tmp_JobInstanceLog ON
GO
IF EXISTS(SELECT * FROM dbo.JobInstanceLog)
	 EXEC('INSERT INTO dbo.Tmp_JobInstanceLog (Id, InstanceId, JobId, JobName, JobGroup, JobType, TriggerId, TriggerName, TriggerGroup, ServerName, Status, StatusTitle, StartDate, EndDate, Duration, EffectedRows, Data, [Log], Exception, Retry, IsStopped, Anomaly)
		SELECT CONVERT(bigint, Id), InstanceId, JobId, JobName, JobGroup, JobType, TriggerId, TriggerName, TriggerGroup, ServerName, Status, StatusTitle, StartDate, EndDate, Duration, EffectedRows, Data, [Log], Exception, Retry, IsStopped, Anomaly FROM dbo.JobInstanceLog WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_JobInstanceLog OFF
GO
DROP TABLE dbo.JobInstanceLog
GO
CREATE TABLE dbo.JobInstanceLog
	(
	Id bigint NOT NULL IDENTITY (1, 1),
	InstanceId varchar(250) NOT NULL,
	JobId varchar(20) NOT NULL,
	JobName varchar(50) NOT NULL,
	JobGroup varchar(50) NOT NULL,
	JobType varchar(50) NOT NULL,
	TriggerId varchar(20) NOT NULL,
	TriggerName varchar(50) NOT NULL,
	TriggerGroup varchar(50) NOT NULL,
	ServerName nvarchar(50) NULL,
	Status int NOT NULL,
	StatusTitle varchar(10) NULL,
	StartDate datetime NOT NULL,
	EndDate datetime NULL,
	Duration int NULL,
	EffectedRows int NULL,
	Data nvarchar(4000) NULL,
	[Log] nvarchar(MAX) NULL,
	Exception nvarchar(MAX) NULL,
	Retry bit NOT NULL,
	IsStopped bit NOT NULL,
	Anomaly tinyint NULL
	) 
GO
ALTER TABLE dbo.JobInstanceLog ADD CONSTRAINT PK_JobInstanceLog PRIMARY KEY CLUSTERED (Id)
GO
ALTER TABLE dbo.JobInstanceLog ADD CONSTRAINT DF_JobInstanceLog_IsStopped DEFAULT ((0)) FOR IsStopped
GO
SET IDENTITY_INSERT dbo.JobInstanceLog ON
INSERT INTO dbo.JobInstanceLog(Id,InstanceId,JobId,JobName,JobGroup,JobType,TriggerId,TriggerName,TriggerGroup,ServerName,Status,StatusTitle,StartDate,EndDate,Duration,EffectedRows,Data,[Log],Exception,Retry,IsStopped,Anomaly) SELECT Id,InstanceId,JobId,JobName,JobGroup,JobType,TriggerId,TriggerName,TriggerGroup,ServerName,Status,StatusTitle,StartDate,EndDate,Duration,EffectedRows,Data,[Log],Exception,Retry,IsStopped,Anomaly FROM dbo.Tmp_JobInstanceLog
SET IDENTITY_INSERT dbo.JobInstanceLog OFF
GO
DROP TABLE dbo.Tmp_JobInstanceLog
