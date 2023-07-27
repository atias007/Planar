CREATE TABLE [dbo].[SecurityAudits](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](500) NOT NULL,
	[Username] [varchar](50) NOT NULL,
	[UserTitle] [nvarchar](101) NOT NULL,
	[DateCreated] [datetime] NOT NULL,
	[IsWarning] [bit] NOT NULL
 CONSTRAINT [PK_SecurityAudits] PRIMARY KEY CLUSTERED (	[Id] ASC))
