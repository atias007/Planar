CREATE TABLE dbo.MonitorCounters
	(
	Id int NOT NULL IDENTITY (1, 1),
	MonitorId int NOT NULL,
	JobId varchar(20) NOT NULL,
	[Counter] int NOT NULL,
	LastUpdate datetime NULL
	) ON [PRIMARY]
GO

ALTER TABLE dbo.MonitorCounters ADD CONSTRAINT
	PK_MonitorCounter PRIMARY KEY CLUSTERED 
	(
	Id
	) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_MonitorCounters ON dbo.MonitorCounters
	(
	JobId,
	MonitorId
	) ON [PRIMARY]

GO

CREATE TABLE dbo.MonitorMute
	(
	Id int NOT NULL IDENTITY (1, 1),
	JobId varchar(20) NULL,
	MonitorId int NULL,
	DueDate datetime NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.MonitorMute ADD CONSTRAINT
	PK_MonitorMute PRIMARY KEY CLUSTERED 
	(
	Id
	) ON [PRIMARY]

GO

CREATE UNIQUE NONCLUSTERED INDEX IX_MonitorMute ON dbo.MonitorMute
	(
	JobId,
	MonitorId
	) ON [PRIMARY]

GO