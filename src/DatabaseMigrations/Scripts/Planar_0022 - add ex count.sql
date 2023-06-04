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
	ExceptionCount int NOT NULL,
	Retry bit NOT NULL,
	IsCanceled bit NOT NULL,
	Anomaly tinyint NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_JobInstanceLog SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_JobInstanceLog ADD CONSTRAINT
	DF_JobInstanceLog_ExceptionCount DEFAULT 0 FOR ExceptionCount
GO
ALTER TABLE dbo.Tmp_JobInstanceLog ADD CONSTRAINT
	DF_JobInstanceLog_IsStopped DEFAULT ((0)) FOR IsCanceled
GO
SET IDENTITY_INSERT dbo.Tmp_JobInstanceLog ON
GO
IF EXISTS(SELECT * FROM dbo.JobInstanceLog)
	 EXEC('INSERT INTO dbo.Tmp_JobInstanceLog (Id, InstanceId, JobId, JobName, JobGroup, JobType, TriggerId, TriggerName, TriggerGroup, ServerName, Status, StatusTitle, StartDate, EndDate, Duration, EffectedRows, Data, [Log], Exception, Retry, IsCanceled, Anomaly)
		SELECT Id, InstanceId, JobId, JobName, JobGroup, JobType, TriggerId, TriggerName, TriggerGroup, ServerName, Status, StatusTitle, StartDate, EndDate, Duration, EffectedRows, Data, [Log], Exception, Retry, IsCanceled, Anomaly FROM dbo.JobInstanceLog WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_JobInstanceLog OFF
GO
DROP TABLE dbo.JobInstanceLog
GO
EXECUTE sp_rename N'dbo.Tmp_JobInstanceLog', N'JobInstanceLog', 'OBJECT' 
GO
ALTER TABLE dbo.JobInstanceLog ADD CONSTRAINT
	PK_JobInstanceLog PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]


GO
/****** Object:  StoredProcedure [dbo].[UpdateJobInstanceLog]    Script Date: 04/06/2023 16:33:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[UpdateJobInstanceLog]
  @InstanceId varchar(250),
  @Status int,
  @StatusTitle varchar(10),
  @EndDate datetime,
  @Duration int,
  @EffectedRows int,
  @Log nvarchar(max),
  @Exception nvarchar(max) = null,
  @ExceptionCount int = 0,
  @IsCanceled bit
  AS
  UPDATE [dbo].[JobInstanceLog] SET
	[Status] = @Status,
	[StatusTitle] = @StatusTitle,
	[EndDate] = @EndDate,
	[Duration] = @Duration,
	[EffectedRows] = @EffectedRows,
	[Log] = @Log,
	[Exception] = @Exception,
	[ExceptionCount] = @ExceptionCount,
	[IsCanceled] = @IsCanceled
WHERE 
	InstanceId = @InstanceId