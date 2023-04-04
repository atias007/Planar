WITH CTE
AS
(
	SELECT DATEPART(HOUR,[RecordDate]) [Hour]
      ,[MaxConcurent]
  FROM [Statistics].[ConcurentExecution]
  WHERE [RecordDate] > DATEADD(DAY, -1, GETDATE())
)
SELECT [Hour], MAX([MaxConcurent]) [MaxConcurent] 
FROM CTE
GROUP BY [Hour]
;

WITH CTE
AS
(
	SELECT DATEPART(DAY,[RecordDate]) [Day]
      ,[MaxConcurent]
  FROM [Statistics].[ConcurentExecution]
  WHERE [RecordDate] > DATEADD(MONTH, -1, GETDATE())
)
SELECT [Day], MAX([MaxConcurent]) [MaxConcurent] 
FROM CTE
GROUP BY [Day]
;
WITH CTE
AS
(
	SELECT DATEPART(MONTH,[RecordDate]) [Month]
      ,[MaxConcurent]
  FROM [Statistics].[ConcurentExecution]
  WHERE [RecordDate] > DATEADD(YEAR, -1, GETDATE())
)
SELECT [Month], MAX([MaxConcurent]) [MaxConcurent] 
FROM CTE
GROUP BY [Month]