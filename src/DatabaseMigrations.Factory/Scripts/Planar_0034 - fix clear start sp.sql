ALTER PROCEDURE [Statistics].[ClearStatistics]
@OverDays int = 365
AS
BEGIN
	SET NOCOUNT ON;

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