SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
GO
CREATE TABLE [Statistics].[JobCounters]
	(
	Id int NOT NULL IDENTITY (1, 1),
	JobId varchar(20) NOT NULL,
	RunDate date NOT NULL,
	TotalRuns int NOT NULL,
	SuccessRetries int NULL,
	FailRetries int NULL,
	Recovers int NULL
	)  ON [PRIMARY]
GO
ALTER TABLE [Statistics].[JobCounters] ADD CONSTRAINT
	PK_JobCounters PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE [Statistics].[JobCounters] SET (LOCK_ESCALATION = TABLE)
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_JobCounters ON [Statistics].JobCounters
	(
	RunDate,
	JobId
	) WITH(  IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO