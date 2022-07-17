CREATE TABLE [dbo].[ClusterServers](
	[Server] [nvarchar](100) NOT NULL,
	[Port] [smallint] NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[JoinDate] [datetime] NOT NULL,
	[HealthCheckDate] [datetime] NULL,
 CONSTRAINT [PK_ClusterServers_1] PRIMARY KEY CLUSTERED 
(
	[Server] ASC,
	[Port] ASC,
	[InstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
