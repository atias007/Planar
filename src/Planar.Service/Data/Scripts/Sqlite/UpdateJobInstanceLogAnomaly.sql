UPDATE [JobInstanceLog] SET
	[Anomaly] = @Anomaly
WHERE 
	InstanceId = @InstanceId