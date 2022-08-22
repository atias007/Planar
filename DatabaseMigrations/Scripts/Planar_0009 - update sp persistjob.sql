GO
/****** Object:  StoredProcedure [dbo].[PersistJobInstanceLog]    Script Date: 21/08/2022 21:52:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[PersistJobInstanceLog]
	@InstanceId varchar(250),
	@Log nvarchar(max),
	@Exception nvarchar(max)
AS
	UPDATE [dbo].[JobInstanceLog] SET
	[Log] = @Log,
	[Exception] = @Exception
WHERE 
	InstanceId = @InstanceId AND Status = -1