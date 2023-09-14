IF EXISTS(SELECT 1 FROM sys.procedures WHERE  Name = N'ClearLogInstance')
BEGIN
  DROP PROCEDURE [dbo].[ClearLogInstance]
END

GO

CREATE PROCEDURE [dbo].[ClearLogInstance]
@OverDays int = 365
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [dbo].[JobInstanceLog]
		WHERE DATEDIFF(DAY, [StartDate], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END