CREATE SCHEMA [Admin]

GO
DROP PROCEDURE [dbo].[FactoryReset]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Admin].[FactoryReset]
AS
delete from  [dbo].[QRTZ_BLOB_TRIGGERS]
delete from  [dbo].[QRTZ_CALENDARS]
delete from [dbo].[QRTZ_CRON_TRIGGERS]
delete from [dbo].[QRTZ_FIRED_TRIGGERS]
delete from [dbo].[QRTZ_LOCKS]
delete from [dbo].[QRTZ_PAUSED_TRIGGER_GRPS]
delete from [dbo].[QRTZ_SCHEDULER_STATE]
delete from [dbo].[QRTZ_SIMPLE_TRIGGERS]
delete from [dbo].[QRTZ_SIMPROP_TRIGGERS]
delete from [dbo].[QRTZ_TRIGGERS]
delete from [dbo].[QRTZ_JOB_DETAILS]

TRUNCATE TABLE [dbo].[GlobalParameters]
TRUNCATE TABLE [dbo].[GlobalParameters_Audit]
TRUNCATE TABLE [dbo].[JobInstanceLog]
TRUNCATE TABLE [dbo].[MonitorActions]
TRUNCATE TABLE [dbo].[Trace]

TRUNCATE TABLE [dbo].[UsersToGroups]
TRUNCATE TABLE [dbo].[Groups]
TRUNCATE TABLE [dbo].[Users]
