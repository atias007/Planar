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
  FROM [JobInstanceLog]
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
LIMIT @PageSize OFFSET ((@PageNumber -1) * @PageSize);