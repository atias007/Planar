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
CREATE TABLE dbo.Tmp_Users
	(
	Id int NOT NULL IDENTITY (1, 1),
	Username varchar(50) NOT NULL,
	Password varbinary(128) NOT NULL,
	Salt varbinary(128) NOT NULL,
	RoleId int NOT NULL,
	FirstName nvarchar(50) NOT NULL,
	LastName nvarchar(50) NULL,
	EmailAddress1 nvarchar(250) NULL,
	EmailAddress2 nvarchar(250) NULL,
	EmailAddress3 nvarchar(250) NULL,
	PhoneNumber1 nvarchar(50) NULL,
	PhoneNumber2 nvarchar(50) NULL,
	PhoneNumber3 nvarchar(50) NULL,
	Reference1 nvarchar(500) NULL,
	Reference2 nvarchar(500) NULL,
	Reference3 nvarchar(500) NULL,
	Reference4 nvarchar(500) NULL,
	Reference5 nvarchar(500) NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_Users SET (LOCK_ESCALATION = TABLE)
GO
SET IDENTITY_INSERT dbo.Tmp_Users ON
GO
IF EXISTS(SELECT * FROM dbo.Users)
	 EXEC('INSERT INTO dbo.Tmp_Users (Id, Username, Password, Salt, FirstName, LastName, EmailAddress1, EmailAddress2, EmailAddress3, PhoneNumber1, PhoneNumber2, PhoneNumber3, Reference1, Reference2, Reference3, Reference4, Reference5)
		SELECT Id, Username, Password, Salt, FirstName, LastName, EmailAddress1, EmailAddress2, EmailAddress3, PhoneNumber1, PhoneNumber2, PhoneNumber3, Reference1, Reference2, Reference3, Reference4, Reference5 FROM dbo.Users WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_Users OFF
GO
ALTER TABLE dbo.UsersToGroups
	DROP CONSTRAINT FK_UsersToGroups_Users
GO
DROP TABLE dbo.Users
GO
EXECUTE sp_rename N'dbo.Tmp_Users', N'Users', 'OBJECT' 
GO
ALTER TABLE dbo.Users ADD CONSTRAINT
	PK_Users PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.Users ADD CONSTRAINT
	FK_Users_Users FOREIGN KEY
	(
	Id
	) REFERENCES dbo.Users
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.UsersToGroups ADD CONSTRAINT
	FK_UsersToGroups_Users FOREIGN KEY
	(
	UserId
	) REFERENCES dbo.Users
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.UsersToGroups SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
