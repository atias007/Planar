ALTER PROCEDURE [Statistics].[StatusCounter]
  @FromDate datetime,
  @ToDate datetime
AS
SELECT 
	 COUNT(CASE [Status] WHEN -1 THEN 1 ELSE NULL END) [Running]
	,COUNT(CASE [Status] WHEN 0 THEN 1 ELSE NULL END) [Success]
	,COUNT(CASE [Status] WHEN 1 THEN 1 ELSE NULL END) [Fail]
FROM [dbo].[JobInstanceLog] 
WHERE 
	(@FromDate IS NULL OR [StartDate] > @FromDate) AND 
	(@ToDate IS NULL OR [StartDate] <= @ToDate)