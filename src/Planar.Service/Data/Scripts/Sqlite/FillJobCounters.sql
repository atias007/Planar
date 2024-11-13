DELETE FROM [JobCounters];
DROP TABLE IF EXISTS CTE;
DROP TABLE IF EXISTS TotalCTE;
DROP TABLE IF EXISTS SucessRetryCTE;
DROP TABLE IF EXISTS FailRetryCTE;
DROP TABLE IF EXISTS RecoverCTE;
DROP TABLE IF EXISTS RecoverCTE;

CREATE TEMPORARY TABLE CTE
AS
SELECT 
	[JobId], date([StartDate]) [RunDate], [Status], [TriggerGroup], [Retry]
FROM 
	[JobInstanceLog]
WHERE
	[EndDate] < date() AND
	[Status] <> -1;

CREATE TEMPORARY TABLE TotalCTE
AS
	SELECT 
		[JobId], [RunDate], Count(*) AS [TotalRuns]
	FROM 
		CTE
	GROUP BY
		[JobId], [RunDate];
	
CREATE TEMPORARY TABLE SucessRetryCTE
AS
	SELECT 
		[JobId], [RunDate], Count(*) AS [SuccessRetries]
	FROM 
		CTE
	WHERE 
		[Status] = 0 AND
		[Retry] = 1
	GROUP BY
		[JobId], [RunDate];

CREATE TEMPORARY TABLE FailRetryCTE
AS
	SELECT 
		[JobId], [RunDate], Count(*) AS [FailRetries]
	FROM 
		CTE
	WHERE 
		[Status] = 1 AND
		[Retry] = 1
	GROUP BY
		[JobId], [RunDate];

CREATE TEMPORARY TABLE RecoverCTE
AS
	SELECT 
		[JobId], [RunDate], Count(*) AS [Recovers]
	FROM 
		CTE
	WHERE 
		[TriggerGroup]='RECOVERING_JOBS'
	GROUP BY
		[JobId], [RunDate];

INSERT INTO [JobCounters]
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
	LEFT OUTER JOIN [RecoverCTE] R ON T.[JobId]=R.[JobId] AND T.[RunDate]=R.[RunDate];
