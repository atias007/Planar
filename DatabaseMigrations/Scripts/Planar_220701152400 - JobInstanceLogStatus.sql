GO
/****** Object:  Table [dbo].[JobInstanceLogStatus]    Script Date: 01/07/2022 15:47:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobInstanceLogStatus](
	[Id] [int] NOT NULL,
	[Title] [nvarchar](50) NOT NULL,
	[Active] [bit] NOT NULL,
	[DisplayOrder] [tinyint] NOT NULL,
 CONSTRAINT [PK_JobInstanceLogStatus] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[JobInstanceLogStatus] ([Id], [Title], [Active], [DisplayOrder]) VALUES (-1, N'Running', 1, 4)
GO
INSERT [dbo].[JobInstanceLogStatus] ([Id], [Title], [Active], [DisplayOrder]) VALUES (0, N'Success', 1, 2)
GO
INSERT [dbo].[JobInstanceLogStatus] ([Id], [Title], [Active], [DisplayOrder]) VALUES (1, N'Fail', 1, 1)
GO
INSERT [dbo].[JobInstanceLogStatus] ([Id], [Title], [Active], [DisplayOrder]) VALUES (2, N'Veto', 1, 3)
GO
ALTER TABLE [dbo].[JobInstanceLogStatus] ADD  CONSTRAINT [DF_JobInstanceLogStatus_Active]  DEFAULT ((1)) FOR [Active]
GO
ALTER TABLE [dbo].[JobInstanceLogStatus] ADD  CONSTRAINT [DF_JobInstanceLogStatus_DisplayOrder]  DEFAULT ((0)) FOR [DisplayOrder]
GO
