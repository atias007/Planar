CREATE PROCEDURE [dbo].[ClearLogInstanceByJob]
@JobId varchar(20),
@OverDays int
AS
BEGIN
	SET NOCOUNT ON;

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