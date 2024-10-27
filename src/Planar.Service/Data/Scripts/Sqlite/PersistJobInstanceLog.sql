UPDATE [JobInstanceLog] SET
	[Log] = @Log,
	[Exception] = @Exception,
	[Duration] = @Duration
WHERE 
	InstanceId = @InstanceId AND Status = -1