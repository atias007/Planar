CREATE OR ALTER PROCEDURE [Statistics].[BuildJobStatistics]
AS

BEGIN TRANSACTION

TRUNCATE TABLE [Statistics].[JobDurationStatistics]
TRUNCATE TABLE [Statistics].[JobEffectedRowsStatistics]
;
WITH CTEDuration
AS
(
	SELECT [JobId], AVG([Duration]) [AvgDuration], STDEV([Duration]) [StdevDuration], COUNT(*) [Rows]
	FROM [dbo].[JobInstanceLog]
	WHERE 
		[Status] = 0 
		AND [IsStopped] = 0
		AND [Anomaly] = 0
	GROUP BY [JobId]
)

INSERT INTO [Statistics].[JobDurationStatistics]
           ([JobId]
           ,[AvgDuration]
           ,[StdevDuration]
		   ,[Rows])

SELECT 
	[JobId], 
	[AvgDuration], 
	[StdevDuration],
	[Rows]
FROM
	CTEDuration
WHERE
	[Rows] > 30 AND
	[AvgDuration] IS NOT NULL AND
	[StdevDuration] IS NOT NULL AND
	[AvgDuration] > 0 AND
	[StdevDuration] > 0

-------------------------------------------------------------------------------------------------------
;
WITH CTEEffectedRows
AS
(
	SELECT [JobId], AVG([EffectedRows]) [AvgEffectedRows], STDEV([EffectedRows]) [StdevEffectedRows], COUNT(*) [Rows]
	FROM [dbo].[JobInstanceLog]
	WHERE 
		[Status] = 0 
		AND [IsStopped] = 0
		AND [Anomaly] = 0
		AND [EffectedRows] IS NOT NULL
	GROUP BY [JobId]
)

INSERT INTO [Statistics].[JobEffectedRowsStatistics]
           ([JobId]
           ,[AvgEffectedRows]
           ,[StdevEffectedRows]
		   ,[Rows])

SELECT 
	[JobId], 
	[AvgEffectedRows], 
	[StdevEffectedRows],
	[Rows]
FROM
	CTEEffectedRows
WHERE
	[Rows] > 30 AND
	[AvgEffectedRows] IS NOT NULL AND
	[StdevEffectedRows] IS NOT NULL AND
	[AvgEffectedRows] > 0 AND
	[StdevEffectedRows] > 0

COMMIT