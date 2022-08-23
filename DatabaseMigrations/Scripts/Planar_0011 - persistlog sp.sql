ALTER PROCEDURE [dbo].[PersistJobInstanceLog]
	@InstanceId varchar(250),
	@Log nvarchar(max),
	@Exception nvarchar(max),
	@Duration int
AS
	UPDATE [dbo].[JobInstanceLog] SET
	[Log] = @Log,
	[Exception] = @Exception,
	[Duration] = @Duration
WHERE 
	InstanceId = @InstanceId AND Status = -1