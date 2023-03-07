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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [Statistics].[ConcurentExecution](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [datetime] NOT NULL,
	[Server] [nvarchar](100) NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[MaxConcurent] [int] NOT NULL,
 CONSTRAINT [PK_ConcurentExecution] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Statistics].[ClearStatistics]
@OverDays int = 365
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [Statistics].[ConcurentQueue]
		WHERE DATEDIFF(DAY, [RecordDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END

	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [Statistics].[ConcurentExecution]
		WHERE DATEDIFF(DAY, [RecordDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END

GO

CREATE PROC [Statistics].[SetMaxConcurentExecution]
AS
DECLARE @today datetime = CONVERT(DATE, GETDATE())
DECLARE @hour tinyint = DATEPART(HOUR, GETDATE())
DECLARE @current datetime = DATEADD(HOUR, @hour, @today)

BEGIN TRANSACTION
;
WITH CTE([RecordDate],[RecordHour],[Server],[InstanceId],[ConcurentValue])
AS
(
SELECT 
	CONVERT(DATETIME, CONVERT(DATE, [RecordDate])) [RecordDate],
	DATEPART(HOUR, [RecordDate]) [RecordHour],
	[Server],
	[InstanceId],
	[ConcurentValue]
  FROM [Statistics].[ConcurentQueue]
  WHERE RecordDate < @current
)


INSERT INTO [Statistics].[ConcurentExecution]
           ([RecordDate]
           ,[Server]
           ,[InstanceId]
           ,[MaxConcurent])
SELECT 
	DATEADD(HOUR,[RecordHour],[RecordDate]) [RecordDate],
	[Server],
	[InstanceId],
	MAX([ConcurentValue]) [ConcurentValue]
FROM CTE
GROUP BY
	[RecordDate],
	[RecordHour],
	[Server],
	[InstanceId]

DELETE FROM [Statistics].[ConcurentQueue]
WHERE RecordDate < @current

COMMIT

GO

CREATE TABLE [Statistics].[MaxDurationExecution] (
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [datetime] NOT NULL,
	[MaxDuration] [int] NOT NULL,
 CONSTRAINT [PK_MaxDurationExecution] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE PROC [Statistics].[SetMaxDurationExecution]
AS

DECLARE @StartDate datetime
SELECT @StartDate = MAX([RecordDate]) FROM [Statistics].[MaxDurationExecution]
SET @StartDate = COALESCE(@StartDate, DATEADD(YEAR, -10, GETDATE()))
SET @StartDate = DATEADD(HOUR, 1, @StartDate)

DECLARE @today datetime = CONVERT(DATE, GETDATE())
DECLARE @hour tinyint = DATEPART(HOUR, GETDATE())
DECLARE @current datetime = DATEADD(HOUR, @hour, @today)

;
WITH CTE([RecordDate],[RecordHour],[Duration])
AS
(
	SELECT 
		CONVERT(DATETIME, CONVERT(DATE, [StartDate])) [RecordDate],
		DATEPART(HOUR, [StartDate]) [RecordHour],
		[Duration]
	FROM 
		[dbo].[JobInstanceLog]
	WHERE 
	    [Duration] IS NOT NULL 
		AND [StartDate] > @StartDate 
		AND [StartDate] < @current
)
INSERT INTO [Statistics].[MaxDurationExecution]
	([RecordDate], [MaxDuration])
SELECT 
	DATEADD(HOUR,[RecordHour],[RecordDate]) [RecordDate],
	MAX([Duration]) [MaxDuration]
FROM CTE
GROUP BY
	[RecordDate],
	[RecordHour]
ORDER BY
	[RecordDate],
	[RecordHour]