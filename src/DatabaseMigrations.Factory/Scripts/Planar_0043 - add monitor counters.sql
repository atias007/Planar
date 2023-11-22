CREATE TABLE dbo.MonitorCounters
	(
	Id int NOT NULL IDENTITY (1, 1),
	MonitorId int NOT NULL,
	JobId nvarchar(20) NOT NULL,
	[Counter] int NOT NULL,
	LastUpdate datetime NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.MonitorCounter ADD CONSTRAINT
	PK_MonitorCounter PRIMARY KEY CLUSTERED 
	(
	Id
	)  ON [PRIMARY]