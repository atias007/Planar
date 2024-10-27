UPDATE [JobInstanceLog] SET
	[Status] = @Status,
	[StatusTitle] = @StatusTitle,
	[EndDate] = @EndDate,
	[Duration] = @Duration,
	[EffectedRows] = @EffectedRows,
	[Log] = @Log,
	[Exception] = @Exception,
	[ExceptionCount] = @ExceptionCount,
	[IsCanceled] = @IsCanceled,
	[HasWarnings] = @HasWarnings
WHERE 
	InstanceId = @InstanceId