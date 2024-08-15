ALTER PROCEDURE [dbo].[GetLastHistoryCallForJob]
 @LastDays int,
 @JobId varchar(20),
 @JobGroup varchar(50),
 @JobType varchar(50),
 @PageNumber int,
 @PageSize int
AS

DECLARE @ReferenceDate as DATE
SELECT @ReferenceDate = CAST(GETDATE()-@LastDays AS DATE)
;
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
	  ,[HasWarnings]
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS row_number
  FROM [dbo].[JobInstanceLog]
)
SELECT
  *
FROM added_row_number
WHERE 
	row_number = 1
	AND StartDate>=@ReferenceDate
	AND (@JobId IS NULL OR JobId = @JobId)
	AND (@JobGroup IS NULL OR JobGroup = @JobGroup)
	AND (@JobType IS NULL OR JobType = @JobType)

ORDER BY StartDate DESC
OFFSET ((@PageNumber -1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY

-- Total Rows
;
WITH added_row_number AS (
  SELECT
       [JobId]
	  ,[JobType]
      ,[JobGroup]
	  ,[StartDate]
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS row_number
  FROM [dbo].[JobInstanceLog]
)
SELECT COUNT(*)
FROM added_row_number
WHERE 
	row_number = 1
	AND StartDate>=@ReferenceDate
	AND (@JobId IS NULL OR JobId = @JobId)
	AND (@JobGroup IS NULL OR JobGroup = @JobGroup)
	AND (@JobType IS NULL OR JobType = @JobType)