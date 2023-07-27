CREATE SCHEMA [Statistics]
GO
CREATE PROCEDURE [Statistics].[StatusCounter]
	@Hours int
AS
SELECT 
	 COUNT(CASE [Status] WHEN -1 THEN 1 ELSE NULL END) [Running]
	,COUNT(CASE [Status] WHEN 0 THEN 1 ELSE NULL END) [Success]
	,COUNT(CASE [Status] WHEN 1 THEN 1 ELSE NULL END) [Fail]
FROM [dbo].[JobInstanceLog] 
WHERE DATEDIFF(HOUR,[StartDate], GETDATE()) <= @Hours