CREATE TABLE [HistoryLastLog](
	[Id] [bigint],
	[InstanceId] [varchar](250) NOT NULL,
	[JobId] [varchar](20) PRIMARY KEY,
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
	[HasWarnings] [bit] NOT NULL
	);