USE [Planar]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [Statistics].[ConcurentQueue](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [datetime] NOT NULL,
	[Server] [nvarchar](100) NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[ConcurentValue] [int] NOT NULL,
 CONSTRAINT [PK_ConcurentQueue] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [Statistics].[ConcurentExecution](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [date] NOT NULL,
	[RecordHour] [tinyint] NOT NULL,
	[Server] [nvarchar](100) NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[MaxConcurent] [int] NOT NULL,
 CONSTRAINT [PK_ConcurentExecution] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE PROC [Statistics].[SetMaxConcurentExecution]
AS
WITH CTE([RecordDate],[RecordHour],[Server],[InstanceId],[ConcurentValue])
AS
(
SELECT 
	CONVERT(DATE, RecordDate) [RecordDate],
	DATEPART(HOUR, RecordDate) [RecordHour],
	[Server],
	[InstanceId],
	[ConcurentValue]
  FROM [Statistics].[ConcurentQueue]
)

SELECT 
	[RecordDate],
	[RecordHour],
	[Server],
	[InstanceId],
	MAX([ConcurentValue]) [ConcurentValue]
FROM CTE
GROUP BY
	[RecordDate],
	[RecordHour],
	[Server],
	[InstanceId]