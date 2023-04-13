SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[UpdateJobInstanceLogAnomaly]
  @InstanceId varchar(250),
  @Anomaly tinyint
  AS
  UPDATE [dbo].[JobInstanceLog] SET
	[Anomaly] = @Anomaly
WHERE 
	InstanceId = @InstanceId