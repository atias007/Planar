CREATE PROCEDURE dbo.ClearTrace
@OverDays int = 365
AS
BEGIN
	SET NOCOUNT ON;

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