ALTER PROCEDURE [Statistics].[ClearStatistics]
@OverDays int = 365
AS
BEGIN
    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [Statistics].[ConcurrentQueue]
		WHERE DATEDIFF(DAY, [RecordDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END

	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [Statistics].[ConcurrentExecution]
		WHERE DATEDIFF(DAY, [RecordDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END

GO

ALTER PROCEDURE [dbo].[ClearTrace]
@OverDays int = 365
AS
BEGIN
    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [dbo].[Trace]
		WHERE DATEDIFF(DAY, [TimeStamp], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END

GO

ALTER PROCEDURE [dbo].[ClearLogInstanceByJob]
@JobId varchar(20),
@OverDays int
AS
BEGIN
    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [dbo].[JobInstanceLog]
		WHERE 
			[JobId] = @JobId
			AND DATEDIFF(DAY, [StartDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END

GO

ALTER   PROCEDURE [dbo].[ClearLogInstance]
@OverDays int = 365
AS
BEGIN
    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [dbo].[JobInstanceLog]
		WHERE DATEDIFF(DAY, [StartDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END