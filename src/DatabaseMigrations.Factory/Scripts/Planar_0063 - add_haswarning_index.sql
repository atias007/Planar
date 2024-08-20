CREATE NONCLUSTERED INDEX IX_JobInstanceLog ON dbo.JobInstanceLog
	(
	HasWarnings
	) ON [PRIMARY]
GO