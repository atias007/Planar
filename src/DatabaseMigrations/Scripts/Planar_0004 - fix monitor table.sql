ALTER TABLE [dbo].[MonitorActions]
ALTER COLUMN [JobId] varchar(50)

GO 

EXEC sp_rename '[dbo].[MonitorActions].[JobId]', 'JobName', 'COLUMN';