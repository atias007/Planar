/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.JobInstanceLog
	DROP CONSTRAINT DF_JobInstanceLog_IsStopped
GO
CREATE TABLE dbo.Tmp_JobInstanceLog
	(
	Id int NOT NULL IDENTITY (1, 1),
	InstanceId varchar(250) NOT NULL,
	JobId varchar(20) NOT NULL,
	JobName varchar(50) NOT NULL,
	JobGroup varchar(50) NOT NULL,
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
	Information nvarchar(MAX) NULL,
	Exception nvarchar(MAX) NULL,
	Retry bit NOT NULL,
	IsStopped bit NOT NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_JobInstanceLog SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_JobInstanceLog ADD CONSTRAINT
	DF_JobInstanceLog_IsStopped DEFAULT ((0)) FOR IsStopped
GO
SET IDENTITY_INSERT dbo.Tmp_JobInstanceLog ON
GO
IF EXISTS(SELECT * FROM dbo.JobInstanceLog)
	 EXEC('INSERT INTO dbo.Tmp_JobInstanceLog (Id, InstanceId, JobId, JobName, JobGroup, TriggerId, TriggerName, TriggerGroup, Status, StatusTitle, StartDate, EndDate, Duration, EffectedRows, Data, Information, Exception, Retry, IsStopped)
		SELECT Id, InstanceId, JobId, JobName, JobGroup, TriggerId, TriggerName, TriggerGroup, Status, StatusTitle, StartDate, EndDate, Duration, EffectedRows, Data, Information, Exception, Retry, IsStopped FROM dbo.JobInstanceLog WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_JobInstanceLog OFF
GO
DROP TABLE dbo.JobInstanceLog
GO
EXECUTE sp_rename N'dbo.Tmp_JobInstanceLog', N'JobInstanceLog', 'OBJECT' 
GO
ALTER TABLE dbo.JobInstanceLog ADD CONSTRAINT
	PK_AutomationTaskCalls PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT
