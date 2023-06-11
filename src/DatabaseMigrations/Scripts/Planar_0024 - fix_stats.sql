USE [Planar]
GO
/****** Object:  StoredProcedure [Statistics].[BuildJobStatistics]    Script Date: 11/06/2023 23:10:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [Statistics].[BuildJobStatistics]
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
		AND [IsCanceled] = 0
		AND [Anomaly] = 0
		AND	DATEDIFF(DAY, [StartDate], GETDATE())<=365
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
		AND [IsCanceled] = 0
		AND [Anomaly] = 0
		AND [EffectedRows] IS NOT NULL
		AND	DATEDIFF(DAY, [StartDate], GETDATE())<=365
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
	[AvgEffectedRows] > 0

COMMIT