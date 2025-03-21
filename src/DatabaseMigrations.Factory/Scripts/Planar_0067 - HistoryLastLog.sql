CREATE TABLE [dbo].[HistoryLastLog](
	[Id] [bigint] NOT NULL,
	[InstanceId] [varchar](250) NOT NULL,
	[JobId] [varchar](20) NOT NULL,
	[JobName] [varchar](50) NOT NULL,
	[JobGroup] [varchar](50) NOT NULL,
	[JobType] [varchar](50) NOT NULL,
	[TriggerId] [varchar](20) NOT NULL,
	[ServerName] [nvarchar](50) NULL,
	[Status] [int] NOT NULL,
	[StatusTitle] [varchar](10) NULL,
	[StartDate] [datetime] NOT NULL,
	[Duration] [int] NULL,
	[EffectedRows] [int] NULL,
	[Retry] [bit] NOT NULL,
	[IsCanceled] [bit] NOT NULL,
	[HasWarnings] [bit] NOT NULL,
 CONSTRAINT [PK_HistoryLastLog] PRIMARY KEY CLUSTERED 
(
	[JobId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

DROP TABLE IF EXISTS  #LastHistoryCallForJob
;
WITH added_row_number AS (

  SELECT
       [Id]
	  ,[InstanceId]
      ,[JobId]
      ,[JobName]
	  ,[JobType]
      ,[JobGroup]
      ,[TriggerId]
	  ,[ServerName]
      ,[Status]
	  ,[StatusTitle]
      ,[StartDate]
      ,[Duration]
      ,[EffectedRows]
	  ,[Retry]
	  ,[IsCanceled]
	  ,[HasWarnings]
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS row_number
  FROM [dbo].[JobInstanceLog]
)
 
SELECT  *
INTO #LastHistoryCallForJob
FROM added_row_number
WHERE 
	row_number = 1 

INSERT INTO HistoryLastLog 
SELECT  
		[Id]
	  ,[InstanceId]
      ,[JobId]
      ,[JobName]
	  ,[JobType]
      ,[JobGroup]
      ,[TriggerId]
	  ,[ServerName]
      ,[Status]
	  ,[StatusTitle]
      ,[StartDate]
      ,[Duration]
      ,[EffectedRows]
	  ,[Retry]
	  ,[IsCanceled]
	  ,[HasWarnings]
FROM #LastHistoryCallForJob
ORDER BY StartDate DESC

DROP TABLE IF EXISTS  #LastHistoryCallForJob


