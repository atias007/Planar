SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.DeleteUser
	@Username varchar(50)
AS
BEGIN
	DECLARE @Id int
	SELECT @Id = [Id] FROM [dbo].[Users] WHERE [Username] = @Username
	DELETE FROM [dbo].[UsersToGroups] WHERE [UserId] = @Id
	DELETE FROM [dbo].[Users] WHERE [Id] = @Id
END
GO
