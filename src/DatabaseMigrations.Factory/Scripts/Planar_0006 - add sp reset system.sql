IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'ResetSystemJobs')
BEGIN
  DROP PROCEDURE [Admin].[ResetSystemJobs]
END

GO

CREATE PROCEDURE [Admin].[ResetSystemJobs]
AS
DELETE FROM [dbo].[QRTZ_TRIGGERS] WHERE JOB_GROUP = '__System'
DELETE FROM [dbo].[QRTZ_JOB_DETAILS] WHERE JOB_GROUP = '__System'