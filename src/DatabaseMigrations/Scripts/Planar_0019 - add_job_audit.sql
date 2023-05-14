CREATE TABLE dbo.JobAudit
	(
	Id int NOT NULL IDENTITY (1, 1),
	JobId varchar(20) NOT NULL,
	DateCreated datetime NOT NULL,
	Username varchar(50) NOT NULL,
	UserTitle nvarchar(101) NOT NULL,
	Description varchar(200) NOT NULL,
	AdditionalInfo nvarchar(4000) NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.JobAudit ADD CONSTRAINT
	PK_JobAudit PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
