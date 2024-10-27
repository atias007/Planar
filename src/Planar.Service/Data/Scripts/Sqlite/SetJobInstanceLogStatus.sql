UPDATE [JobInstanceLog] SET
	[Status] = @Status,
	[StatusTitle] = @StatusTitle
WHERE 
	InstanceId = @InstanceId