CREATE TABLE [dbo].[MonitorActionsHooks](
	[MonitorId] [int] NOT NULL,
	[Hook] nvarchar(50) NOT NULL,
 CONSTRAINT [PK_MonitorActionsHooks] PRIMARY KEY CLUSTERED 
(
	[MonitorId] ASC,
	[Hook] ASC
)) ON [PRIMARY]

GO

INSERT INTO [dbo].[MonitorActionsHooks] ([MonitorId], [Hook])
SELECT Id, Hook
FROM [dbo].[MonitorActions]

GO

ALTER TABLE dbo.[MonitorActionsHooks] ADD CONSTRAINT
	FK_MonitorActionsHooks_MonitorActions FOREIGN KEY
	(
	MonitorId
	) REFERENCES dbo.MonitorActions
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO

ALTER TABLE dbo.MonitorActions DROP COLUMN Hook
GO