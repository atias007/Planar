USE [Planar]
GO

/****** Object:  Table [Statistics].[ConcurentQueue]    Script Date: 24/02/2023 13:46:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [Statistics].[ConcurentQueue](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RecordDate] [datetime] NOT NULL,
	[Server] [nvarchar](100) NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[ConcurentValue] [int] NOT NULL,
 CONSTRAINT [PK_ConcurentQueue] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


