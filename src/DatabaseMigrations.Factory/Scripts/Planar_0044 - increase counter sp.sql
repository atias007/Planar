IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'IncreaseMonitorCounter')
BEGIN
  DROP PROCEDURE dbo.IncreaseMonitorCounter
END

GO


CREATE PROCEDURE dbo.IncreaseMonitorCounter
  @JobId nvarchar(20),
  @MonitorId int
AS

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;  
BEGIN TRANSACTION;  

UPDATE [dbo].[MonitorCounters] SET 
	[Counter] = [Counter] + 1,
	[LastUpdate] = GETDATE()
WHERE [JobId] = @JobId AND [MonitorId]=@MonitorId

COMMIT
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 

GO

