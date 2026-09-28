IF OBJECT_ID(N'dbo.Resources', N'U') IS NULL
BEGIN

CREATE TABLE [dbo].[Resources](
	[Name] [nvarchar](100) NOT NULL PRIMARY KEY,
	[Value] [nvarchar](max) NOT NULL
)

END