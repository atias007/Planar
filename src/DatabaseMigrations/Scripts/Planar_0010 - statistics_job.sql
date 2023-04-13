CREATE TABLE [Statistics].[JobDurationStatistics]
	(
	JobId varchar(20) NOT NULL,
	AvgDuration numeric(18, 4) NOT NULL,
	StdevDuration numeric(18, 4) NOT NULL,
	[Rows] int  NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE [Statistics].[JobDurationStatistics] ADD CONSTRAINT
	PK_JobDurationStatistics PRIMARY KEY CLUSTERED 
	(
	JobId
	) WITH(IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE [Statistics].[JobDurationStatistics] SET (LOCK_ESCALATION = TABLE)
GO

CREATE TABLE [Statistics].[JobEffectedRowsStatistics]
	(
	JobId varchar(20) NOT NULL,
	AvgEffectedRows numeric(18, 4) NOT NULL,
	StdevEffectedRows numeric(18, 4) NOT NULL,
	[Rows] int  NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE [Statistics].JobEffectedRowsStatistics ADD CONSTRAINT
	PK_JobEffectedRowsStatistics PRIMARY KEY CLUSTERED 
	(
	JobId
	) WITH(IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE [Statistics].[JobEffectedRowsStatistics] SET (LOCK_ESCALATION = TABLE)
GO

ALTER TABLE [dbo].[JobInstanceLog] ADD [Anomaly] tinyint NULL