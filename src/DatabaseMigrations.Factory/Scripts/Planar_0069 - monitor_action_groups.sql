CREATE TABLE [dbo].[MonitorActionsGroups](
	[MonitorId] [int] NOT NULL,
	[GroupId] [int] NOT NULL,
 CONSTRAINT [PK_MonitorActionsGroups] PRIMARY KEY CLUSTERED 
(
	[MonitorId] ASC,
	[GroupId] ASC
)) ON [PRIMARY]

GO

INSERT INTO [dbo].[MonitorActionsGroups] ([MonitorId], [GroupId])
SELECT Id, GroupId
FROM [dbo].[MonitorActions]

GO

ALTER TABLE dbo.[MonitorActionsGroups] ADD CONSTRAINT
	FK_MonitorActionsGroups_MonitorActions FOREIGN KEY
	(
	MonitorId
	) REFERENCES dbo.MonitorActions
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.[MonitorActionsGroups] ADD CONSTRAINT
	FK_MonitorActionsGroups_Groups FOREIGN KEY
	(
	GroupId
	) REFERENCES dbo.Groups
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO

ALTER TABLE dbo.MonitorActions DROP CONSTRAINT FK_MonitorActions_Groups
GO

ALTER TABLE dbo.MonitorActions DROP COLUMN GroupId
GO