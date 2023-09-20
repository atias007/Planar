IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'UpdateJobInstanceLogAnomaly')
BEGIN
  DROP PROCEDURE  [dbo].[UpdateJobInstanceLogAnomaly]
END

GO

CREATE PROCEDURE [dbo].[UpdateJobInstanceLogAnomaly]
  @InstanceId varchar(250),
  @Anomaly tinyint
  AS
  UPDATE [dbo].[JobInstanceLog] SET
	[Anomaly] = @Anomaly
WHERE 
	InstanceId = @InstanceId