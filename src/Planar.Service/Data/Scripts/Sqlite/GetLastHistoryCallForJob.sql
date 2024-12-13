DROP TABLE IF EXISTS  temp.LastHistoryCallForJob;

CREATE TABLE temp.LastHistoryCallForJob(
	[Id] [INTEGER] PRIMARY KEY AUTOINCREMENT,
	[JobId] [varchar](20) NOT NULL,
	[JobName] [varchar](50) NOT NULL,
	[JobGroup] [varchar](50) NOT NULL,
	[JobType] [varchar](50) NOT NULL,
	[TriggerId] [varchar](20) NOT NULL,
	[Status] [int] NOT NULL,
	[StatusTitle] [varchar](10) NULL,
	[StartDate] [datetime] NOT NULL,
	[Duration] [int] NULL,
	[EffectedRows] [int] NULL,
	[HasWarnings] [bit] NOT NULL,
	[Num] [int] NOT NULL
	);

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
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS [Num]
  FROM [JobInstanceLog]
)

INSERT INTO temp.LastHistoryCallForJob(
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
	  ,[Num])
SELECT * FROM added_row_number
WHERE 	Num = 1
ORDER BY StartDate DESC;

SELECT  * FROM temp.LastHistoryCallForJob
WHERE 
	[Num] = 1
	AND StartDate>=@ReferenceDate
	AND (@JobId IS NULL OR JobId = @JobId)
	AND (@JobGroup IS NULL OR JobGroup = @JobGroup)
	AND (@JobType IS NULL OR JobType = @JobType)
ORDER BY StartDate DESC
LIMIT {{limit}} OFFSET {{offset}};

SELECT COUNT(*) FROM temp.LastHistoryCallForJob;

DROP TABLE IF EXISTS  temp.LastHistoryCallForJob;