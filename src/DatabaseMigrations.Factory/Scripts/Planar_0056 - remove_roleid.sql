ALTER TABLE [dbo].[Groups] DROP CONSTRAINT [DF_Groups_RoleId]
GO
ALTER TABLE dbo.Groups DROP COLUMN RoleId
GO
ALTER TABLE dbo.Groups ADD [Role] varchar(20)
GO
UPDATE dbo.Groups SET [Role]='anonymous'
GO
ALTER TABLE dbo.Groups ALTER COLUMN [Role] varchar(20) NOT NULL