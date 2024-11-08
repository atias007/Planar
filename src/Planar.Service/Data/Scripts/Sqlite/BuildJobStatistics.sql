--BEGIN TRANSACTION;

DELETE FROM [JobDurationStatistics];
DELETE FROM  [JobEffectedRowsStatistics];

WITH CTEDuration
AS
(
	SELECT [JobId], AVG([Duration]) [AvgDuration], 0 AS [StdevDuration], COUNT(*) [Rows]
	FROM [JobInstanceLog]
	WHERE 
		[Status] = 0 
		AND [IsCanceled] = 0
		AND [Anomaly] BETWEEN 0 AND 10
		AND	julianday('now', 'localtime') - julianday(StartDate, 'localtime') <=365
	GROUP BY [JobId]
)

INSERT INTO [JobDurationStatistics]
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
	[StdevDuration] > 0;

-------------------------------------------------------------------------------------------------------
WITH CTEEffectedRows
AS
(
	SELECT [JobId], AVG([EffectedRows]) [AvgEffectedRows], 0 AS [StdevEffectedRows], COUNT(*) [Rows]
	FROM [JobInstanceLog]
	WHERE 
		[Status] = 0 
		AND [IsCanceled] = 0
		AND [Anomaly] BETWEEN 0 AND 10
		AND [EffectedRows] IS NOT NULL
		AND	julianday('now', 'localtime') - julianday(StartDate, 'localtime') <=365
	GROUP BY [JobId]
)

INSERT INTO [JobEffectedRowsStatistics]
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
	[AvgEffectedRows] > 0

--COMMIT;
