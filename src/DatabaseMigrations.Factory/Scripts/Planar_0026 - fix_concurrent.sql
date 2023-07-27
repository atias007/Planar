UPDATE [dbo].[QRTZ_JOB_DETAILS] SET [JOB_CLASS_NAME]=REPLACE(JOB_CLASS_NAME,'Concurent','Concurrent')
GO
EXEC sp_rename 'Statistics.ConcurentQueue.ConcurentValue', 'ConcurrentValue', 'COLUMN';
GO
EXEC sp_rename 'Statistics.ConcurentExecution.MaxConcurent', 'MaxConcurrent', 'COLUMN';
GO
EXEC sp_rename 'Statistics.ConcurentQueue', 'ConcurrentQueue';
GO
EXEC sp_rename 'Statistics.ConcurentExecution', 'ConcurrentExecution';
GO
DROP PROC [Statistics].[SetMaxConcurentExecution]
GO
CREATE PROC [Statistics].[SetMaxConcurrentExecution]
AS
DECLARE @today datetime = CONVERT(DATE, GETDATE())
DECLARE @hour tinyint = DATEPART(HOUR, GETDATE())
DECLARE @current datetime = DATEADD(HOUR, @hour, @today)

BEGIN TRANSACTION
;
WITH CTE([RecordDate],[RecordHour],[Server],[InstanceId],[ConcurrentValue])
AS
(
SELECT 
	CONVERT(DATETIME, CONVERT(DATE, RecordDate)) [RecordDate],
	DATEPART(HOUR, RecordDate) [RecordHour],
	[Server],
	[InstanceId],
	[ConcurrentValue]
  FROM [Statistics].[ConcurrentQueue]
  WHERE RecordDate < @current
)


INSERT INTO [Statistics].[ConcurrentExecution]
           ([RecordDate]
           ,[Server]
           ,[InstanceId]
           ,[MaxConcurrent])
SELECT 
	DATEADD(HOUR,[RecordHour],[RecordDate]) [RecordDate],
	[Server],
	[InstanceId],
	MAX([ConcurrentValue]) [ConcurrentValue]
FROM CTE
GROUP BY
	[RecordDate],
	[RecordHour],
	[Server],
	[InstanceId]

DELETE FROM [Statistics].[ConcurrentQueue]
WHERE RecordDate < @current

COMMIT

GO