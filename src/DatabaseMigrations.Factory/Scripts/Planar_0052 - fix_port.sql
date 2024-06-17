CREATE TABLE dbo.Tmp_ClusterNodes
	(
	Server nvarchar(100) NOT NULL,
	Port int NOT NULL,
	InstanceId nvarchar(100) NOT NULL,
	ClusterPort smallint NOT NULL,
	JoinDate datetime NOT NULL,
	HealthCheckDate datetime NOT NULL,
	MaxConcurrency int NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_ClusterNodes SET (LOCK_ESCALATION = TABLE)
GO
IF EXISTS(SELECT * FROM dbo.ClusterNodes)
	 EXEC('INSERT INTO dbo.Tmp_ClusterNodes (Server, Port, InstanceId, ClusterPort, JoinDate, HealthCheckDate, MaxConcurrency)
		SELECT Server, CONVERT(int, Port), InstanceId, ClusterPort, JoinDate, HealthCheckDate, MaxConcurrency FROM dbo.ClusterNodes WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.ClusterNodes
GO
EXECUTE sp_rename N'dbo.Tmp_ClusterNodes', N'ClusterNodes', 'OBJECT' 
GO
ALTER TABLE dbo.ClusterNodes ADD CONSTRAINT
	PK_ClusterNodes PRIMARY KEY CLUSTERED 
	(
	Server,
	Port
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.ClusterNodes ADD CONSTRAINT
	IX_ClusterNodes UNIQUE NONCLUSTERED 
	(
	InstanceId
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO