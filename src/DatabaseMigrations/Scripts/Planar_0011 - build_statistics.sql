CREATE OR ALTER PROCEDURE [Statistics].[BuildJobStatistics]
AS

BEGIN TRANSACTION

TRUNCATE TABLE [Statistics].[JobStatistics]
;
WITH CTEDuration
AS
(
	SELECT [JobId], AVG([Duration]) [AvgDuration], STDEV([Duration]) [StdevDuration], COUNT(*) [Rows]
	FROM [dbo].[JobInstanceLog]
	WHERE 
		[Status] = 0 
		AND [IsStopped] = 0
		AND ([Anomaly] IS NULL OR [Anomaly] = 0)
	GROUP BY [JobId]
),
CTEEffectedRows
AS
(
	SELECT [JobId], AVG([EffectedRows]) [AvgEffectedRows], STDEV([EffectedRows]) [StdevEffectedRows]
	FROM [dbo].[JobInstanceLog]
	WHERE 
		[Status] = 0 
		AND [IsStopped] = 0
		AND ([Anomaly] IS NULL OR [Anomaly] = 0)
		AND [EffectedRows] IS NOT NULL
	GROUP BY [JobId]
)

INSERT INTO [Statistics].[JobStatistics]
           ([JobId]
           ,[AvgDuration]
           ,[StdevDuration]
           ,[AvgEffectedRows]
           ,[StdevEffectedRows]
		   ,[Rows])

SELECT 
	D.[JobId], 
	D.[AvgDuration], 
	D.[StdevDuration],
	E.[AvgEffectedRows],
	E.[StdevEffectedRows], 
	D.[Rows]
FROM
	CTEDuration D LEFT OUTER JOIN
	CTEEffectedRows E ON D.[JobId]=E.[JobId]
WHERE
	D.[Rows] > 30 AND
	D.[AvgDuration] IS NOT NULL AND
	D.[StdevDuration] IS NOT NULL AND
	D.[AvgDuration] > 0 AND
	D.[StdevDuration] > 0

COMMIT