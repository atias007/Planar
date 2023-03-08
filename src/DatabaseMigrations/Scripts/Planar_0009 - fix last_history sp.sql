SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--exec dbo.GetLastHistoryCallForJob 7

ALTER PROCEDURE [dbo].[GetLastHistoryCallForJob]
 @LastDays int 
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