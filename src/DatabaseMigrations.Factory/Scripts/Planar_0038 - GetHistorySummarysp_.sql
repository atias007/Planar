IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'GetHistorySummary')
BEGIN
  DROP PROCEDURE dbo.GetHistorySummary
END

GO

CREATE PROCEDURE dbo.GetHistorySummary
  @FromDate datetime,
  @ToDate datetime,
  @PageNumber int,
  @PageSize int
AS
SELECT 
       [JobId]
      ,[JobName]
      ,[JobGroup]
      ,[JobType]
	  ,COUNT(*) [TotalRuns]
      ,SUM(CASE [Status] WHEN 0 THEN 1 ELSE 0 END) [Success]
	  ,SUM(CASE [Status] WHEN 1 THEN 1 ELSE 0 END) [Fail]
	  ,SUM(CASE [Status] WHEN -1 THEN 1 ELSE 0 END) [Running]
	  ,SUM(CASE [Retry] WHEN 1 THEN 1 ELSE 0 END) [Retries]
  FROM [Planar].[dbo].[JobInstanceLog]
  WHERE 
	(@FromDate IS NULL OR [StartDate] > @FromDate) AND 
    (@ToDate IS NULL OR [StartDate] <= @ToDate)
  GROUP BY
       [JobId]
      ,[JobName]
      ,[JobGroup]
      ,[JobType]
  ORDER BY 
      [JobGroup],
	  [JobName]
OFFSET ((@PageNumber -1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY

;
WITH TOTALCTE AS
(
SELECT 
	  COUNT(*) [TotalRuns]
  FROM [Planar].[dbo].[JobInstanceLog]
  WHERE 
	(@FromDate IS NULL OR [StartDate] > @FromDate) AND 
    (@ToDate IS NULL OR [StartDate] <= @ToDate)
  GROUP BY
       [JobId]
      ,[JobName]
      ,[JobGroup]
      ,[JobType]
)
SELECT COUNT(*) FROM TOTALCTE