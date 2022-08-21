SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[UpdateJobInstanceLog]
  @InstanceId varchar(250),
  @Status int,
  @StatusTitle varchar(10),
  @EndDate datetime,
  @Duration int,
  @EffectedRows int,
  @Log nvarchar(max),
  @Exception nvarchar(max) = null,
  @IsStopped bit
  AS
  UPDATE [dbo].[JobInstanceLog] SET
	[Status] = @Status,
	[StatusTitle] = @StatusTitle,
	[EndDate] = @EndDate,
	[Duration] = @Duration,
	[EffectedRows] = @EffectedRows,
	[Log] = @Log,
	[Exception] = @Exception,
	[IsStopped] = @IsStopped
WHERE 
	InstanceId = @InstanceId