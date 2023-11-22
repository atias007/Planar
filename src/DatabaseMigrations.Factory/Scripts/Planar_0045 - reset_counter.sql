IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'ResetMonitorCounter')
BEGIN
  DROP PROCEDURE dbo.ResetMonitorCounter
END

GO


CREATE PROCEDURE [dbo].[ResetMonitorCounter]
  @Delta nvarchar(20)
AS
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;  
BEGIN TRANSACTION; 

  UPDATE [Planar].[dbo].[MonitorCounters]
  SET [Counter] = 0,
  [LastUpdate] = GETDATE()
  WHERE DATEDIFF(MINUTE, [LastUpdate], GETDATE()) > @Delta

COMMIT
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 