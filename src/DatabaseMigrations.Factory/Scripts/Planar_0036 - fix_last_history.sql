ALTER PROCEDURE [dbo].[GetLastHistoryCallForJob]
 @LastDays int,
 @PageNumber int,
 @PageSize int
AS
WITH added_row_number AS (
  SELECT
       [Id]
      ,[JobId]
      ,[JobName]
	  ,[JobType]
      ,[JobGroup]
      ,[TriggerId]
      ,[Status]
	  ,[StatusTitle]
      ,[StartDate]
      ,[Duration]
      ,[EffectedRows]
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS row_number
  FROM [dbo].[JobInstanceLog]
)
SELECT
  *
FROM added_row_number
WHERE row_number = 1
AND DATEDIFF(day, StartDate, GETDATE())<=@LastDays
ORDER BY StartDate DESC
OFFSET ((@PageNumber -1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY

-- Total Rows
;
WITH added_row_number AS (
  SELECT
       [StartDate]
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS row_number
  FROM [dbo].[JobInstanceLog]
)
SELECT COUNT(*)
FROM added_row_number
WHERE row_number = 1
AND DATEDIFF(day, StartDate, GETDATE())<=@LastDays