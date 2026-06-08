CREATE TABLE [dbo].[Agents](
	[ClientId] [nvarchar](100) NOT NULL,
	[IpAddress] [varchar](50) NOT NULL,
	[LastSeen] [datetime] NOT NULL,
 CONSTRAINT [PK_Agents] PRIMARY KEY CLUSTERED 
(
	[ClientId] ASC
))