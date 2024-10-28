SELECT 
     COUNT(CASE [Level] WHEN 'Fatal' THEN 1 ELSE NULL END) [Fatal]
	,COUNT(CASE [Level] WHEN 'Error' THEN 1 ELSE NULL END) [Error]
	,COUNT(CASE [Level] WHEN 'Warning' THEN 1 ELSE NULL END) [Warning]
	,COUNT(CASE [Level] WHEN 'Information' THEN 1 ELSE NULL END) [Information]
	,COUNT(CASE [Level] WHEN 'Debug' THEN 1 ELSE NULL END) [Debug]
	,COUNT(CASE [Level] WHEN 'Trace' THEN 1 ELSE NULL END) [Trace]
FROM [Trace] 
WHERE 
  (@FromDate IS NULL OR [TimeStamp] > @FromDate) AND 
  (@ToDate IS NULL OR [TimeStamp] <= @ToDate)