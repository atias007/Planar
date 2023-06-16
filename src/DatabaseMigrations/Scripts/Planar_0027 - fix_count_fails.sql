ALTER PROCEDURE	[dbo].[CountFailsInHourForJob]
	@JobId varchar(20),
	@Hours int = 1
AS

SELECT COUNT([Id])
FROM 
	[dbo].[JobInstanceLog]
WHERE 
	[JobId] = @JobId AND
	[Status] = 1 AND
	DATEDIFF(MINUTE, [EndDate], GETDATE()) <= (@Hours * 60)