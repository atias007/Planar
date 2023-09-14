IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'FillJobCounters')
BEGIN
  DROP PROCEDURE [Statistics].[FillJobCounters]
END

GO

CREATE PROCEDURE [Statistics].[FillJobCounters]
AS
TRUNCATE TABLE [Statistics].[JobCounters]

DECLARE @today datetime = CONVERT(DATE, GETDATE())
;
WITH CTE
AS
(
SELECT 
	[JobId], CONVERT(DATE, [StartDate]) [RunDate], [Status], [TriggerGroup], [Retry]
FROM 
	[dbo].[JobInstanceLog]
WHERE
	[EndDate] < @today AND
	[Status] <> -1 
)
,TotalCTE
AS
(
	SELECT 
		[JobId], [RunDate], Count(*) AS [TotalRuns]
	FROM 
		CTE
	GROUP BY
		[JobId], [RunDate]
)
,SucessRetryCTE
AS
(
	SELECT 
		[JobId], [RunDate], Count(*) AS [SuccessRetries]
	FROM 
		CTE
	WHERE 
		[Status] = 0 AND
		[Retry] = 1
	GROUP BY
		[JobId], [RunDate]

)
,FailRetryCTE
AS
(
	SELECT 
		[JobId], [RunDate], Count(*) AS [FailRetries]
	FROM 
		CTE
	WHERE 
		[Status] = 1 AND
		[Retry] = 1
	GROUP BY
		[JobId], [RunDate]

)
,RecoverCTE
AS
(
	SELECT 
		[JobId], [RunDate], Count(*) AS [Recovers]
	FROM 
		CTE
	WHERE 
		[TriggerGroup]='RECOVERING_JOBS'
	GROUP BY
		[JobId], [RunDate]

)

INSERT INTO [Statistics].[JobCounters]
           ([JobId]
           ,[RunDate]
           ,[TotalRuns]
		   ,[SuccessRetries]
		   ,[FailRetries]
		   ,[Recovers])
SELECT 
	T.[JobId], 
	T.[RunDate], 
	T.[TotalRuns],
	S.[SuccessRetries],
	F.[FailRetries],
	R.[Recovers]
FROM
	[TotalCTE] T
	LEFT OUTER JOIN [SucessRetryCTE] S ON T.[JobId]=S.[JobId] AND T.[RunDate]=S.[RunDate]
	LEFT OUTER JOIN [FailRetryCTE] F ON T.[JobId]=F.[JobId] AND T.[RunDate]=F.[RunDate]
	LEFT OUTER JOIN [RecoverCTE] R ON T.[JobId]=R.[JobId] AND T.[RunDate]=R.[RunDate]
