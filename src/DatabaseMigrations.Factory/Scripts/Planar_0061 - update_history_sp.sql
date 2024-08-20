ALTER PROCEDURE [dbo].[UpdateJobInstanceLog]
  @InstanceId varchar(250),
  @Status int,
  @StatusTitle varchar(10),
  @EndDate datetime,
  @Duration int,
  @EffectedRows int,
  @Log nvarchar(max),
  @Exception nvarchar(max) = null,
  @ExceptionCount int = 0,
  @IsCanceled bit,
  @HasWarnings bit
  AS
  UPDATE [dbo].[JobInstanceLog] SET
	[Status] = @Status,
	[StatusTitle] = @StatusTitle,
	[EndDate] = @EndDate,
	[Duration] = @Duration,
	[EffectedRows] = @EffectedRows,
	[Log] = @Log,
	[Exception] = @Exception,
	[ExceptionCount] = @ExceptionCount,
	[IsCanceled] = @IsCanceled,
	[HasWarnings]=@HasWarnings
WHERE 
	InstanceId = @InstanceId