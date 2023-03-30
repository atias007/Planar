CREATE TABLE [Statistics].[JobStatistics]
	(
	JobId varchar(20) NOT NULL,
	AvgDuration numeric(18, 4) NOT NULL,
	StdevDuration numeric(18, 4) NOT NULL,
	AvgEffectedRows numeric(18, 4) NULL,
	StdevEffectedRows numeric(18, 4) NULL,
	[Rows] int  NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE [Statistics].JobStatistics ADD CONSTRAINT
	PK_JobStatistics PRIMARY KEY CLUSTERED 
	(
	JobId
	) WITH(IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE [Statistics].JobStatistics SET (LOCK_ESCALATION = TABLE)
GO

ALTER TABLE [dbo].[JobInstanceLog] ADD [Anomaly] bit NULL