CREATE PROCEDURE [Statistics].[TraceCounter]
	@Hours int
AS
SELECT 
     COUNT(CASE [Level] WHEN 'Fatal' THEN 1 ELSE NULL END) [Fatal]
	,COUNT(CASE [Level] WHEN 'Error' THEN 1 ELSE NULL END) [Error]
	,COUNT(CASE [Level] WHEN 'Warning' THEN 1 ELSE NULL END) [Warning]
	,COUNT(CASE [Level] WHEN 'Information' THEN 1 ELSE NULL END) [Information]
	,COUNT(CASE [Level] WHEN 'Debug' THEN 1 ELSE NULL END) [Debug]
	
FROM [dbo].[Trace] 
WHERE DATEDIFF(HOUR,[TimeStamp], GETDATE()) <= @Hours