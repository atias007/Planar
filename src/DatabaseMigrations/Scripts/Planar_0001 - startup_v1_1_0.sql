USE [Planar]
GO
/****** Object:  Schema [Admin]    Script Date: 15/01/2023 0:44:07 ******/
CREATE SCHEMA [Admin]
GO
/****** Object:  Table [dbo].[ClusterNodes]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ClusterNodes](
	[Server] [nvarchar](100) NOT NULL,
	[Port] [smallint] NOT NULL,
	[InstanceId] [nvarchar](100) NOT NULL,
	[ClusterPort] [smallint] NOT NULL,
	[JoinDate] [datetime] NOT NULL,
	[HealthCheckDate] [datetime] NOT NULL,
 CONSTRAINT [PK_ClusterNodes] PRIMARY KEY CLUSTERED 
(
	[Server] ASC,
	[Port] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_ClusterNodes] UNIQUE NONCLUSTERED 
(
	[InstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GlobalConfig]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GlobalConfig](
	[Key] [nvarchar](50) NOT NULL,
	[Value] [nvarchar](1000) NOT NULL,
	[Type] [varchar](10) NOT NULL,
 CONSTRAINT [PK_GlobalConfig] PRIMARY KEY CLUSTERED 
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Groups]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Groups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Reference1] [nvarchar](500) NULL,
	[Reference2] [nvarchar](500) NULL,
	[Reference3] [nvarchar](500) NULL,
	[Reference4] [nvarchar](500) NULL,
	[Reference5] [nvarchar](500) NULL,
	[RoleId] [int] NOT NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JobInstanceLog]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobInstanceLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InstanceId] [varchar](250) NOT NULL,
	[JobId] [varchar](20) NOT NULL,
	[JobName] [varchar](50) NOT NULL,
	[JobGroup] [varchar](50) NOT NULL,
	[TriggerId] [varchar](20) NOT NULL,
	[TriggerName] [varchar](50) NOT NULL,
	[TriggerGroup] [varchar](50) NOT NULL,
	[ServerName] [nvarchar](50) NULL,
	[Status] [int] NOT NULL,
	[StatusTitle] [varchar](10) NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NULL,
	[Duration] [int] NULL,
	[EffectedRows] [int] NULL,
	[Data] [nvarchar](4000) NULL,
	[Log] [nvarchar](max) NULL,
	[Exception] [nvarchar](max) NULL,
	[Retry] [bit] NOT NULL,
	[IsStopped] [bit] NOT NULL,
 CONSTRAINT [PK_AutomationTaskCalls] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JobProperties]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobProperties](
	[JobId] [varchar](20) NOT NULL,
	[Properties] [nvarchar](max) NULL,
 CONSTRAINT [PK_JobProperties] PRIMARY KEY CLUSTERED 
(
	[JobId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MonitorActions]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MonitorActions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](50) NOT NULL,
	[EventId] [int] NOT NULL,
	[EventArgument] [varchar](50) NULL,
	[JobName] [varchar](50) NULL,
	[JobGroup] [varchar](50) NULL,
	[GroupId] [int] NOT NULL,
	[Hook] [varchar](50) NOT NULL,
	[Active] [bit] NOT NULL,
 CONSTRAINT [PK_Monitor] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_BLOB_TRIGGERS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_BLOB_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](150) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
	[BLOB_DATA] [varbinary](max) NULL,
 CONSTRAINT [PK_QRTZ_BLOB_TRIGGERS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_CALENDARS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_CALENDARS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[CALENDAR_NAME] [nvarchar](200) NOT NULL,
	[CALENDAR] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_QRTZ_CALENDARS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[CALENDAR_NAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_CRON_TRIGGERS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_CRON_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](150) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
	[CRON_EXPRESSION] [nvarchar](120) NOT NULL,
	[TIME_ZONE_ID] [nvarchar](80) NULL,
 CONSTRAINT [PK_QRTZ_CRON_TRIGGERS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_FIRED_TRIGGERS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_FIRED_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[ENTRY_ID] [nvarchar](140) NOT NULL,
	[TRIGGER_NAME] [nvarchar](150) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
	[INSTANCE_NAME] [nvarchar](200) NOT NULL,
	[FIRED_TIME] [bigint] NOT NULL,
	[SCHED_TIME] [bigint] NOT NULL,
	[PRIORITY] [int] NOT NULL,
	[STATE] [nvarchar](16) NOT NULL,
	[JOB_NAME] [nvarchar](150) NULL,
	[JOB_GROUP] [nvarchar](150) NULL,
	[IS_NONCONCURRENT] [bit] NULL,
	[REQUESTS_RECOVERY] [bit] NULL,
 CONSTRAINT [PK_QRTZ_FIRED_TRIGGERS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[ENTRY_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_JOB_DETAILS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_JOB_DETAILS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[JOB_NAME] [nvarchar](150) NOT NULL,
	[JOB_GROUP] [nvarchar](150) NOT NULL,
	[DESCRIPTION] [nvarchar](250) NULL,
	[JOB_CLASS_NAME] [nvarchar](250) NOT NULL,
	[IS_DURABLE] [bit] NOT NULL,
	[IS_NONCONCURRENT] [bit] NOT NULL,
	[IS_UPDATE_DATA] [bit] NOT NULL,
	[REQUESTS_RECOVERY] [bit] NOT NULL,
	[JOB_DATA] [varbinary](max) NULL,
 CONSTRAINT [PK_QRTZ_JOB_DETAILS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[JOB_NAME] ASC,
	[JOB_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_LOCKS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_LOCKS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[LOCK_NAME] [nvarchar](40) NOT NULL,
 CONSTRAINT [PK_QRTZ_LOCKS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[LOCK_NAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_PAUSED_TRIGGER_GRPS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_PAUSED_TRIGGER_GRPS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
 CONSTRAINT [PK_QRTZ_PAUSED_TRIGGER_GRPS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_SCHEDULER_STATE]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_SCHEDULER_STATE](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[INSTANCE_NAME] [nvarchar](200) NOT NULL,
	[LAST_CHECKIN_TIME] [bigint] NOT NULL,
	[CHECKIN_INTERVAL] [bigint] NOT NULL,
 CONSTRAINT [PK_QRTZ_SCHEDULER_STATE] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[INSTANCE_NAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_SIMPLE_TRIGGERS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](150) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
	[REPEAT_COUNT] [int] NOT NULL,
	[REPEAT_INTERVAL] [bigint] NOT NULL,
	[TIMES_TRIGGERED] [int] NOT NULL,
 CONSTRAINT [PK_QRTZ_SIMPLE_TRIGGERS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_SIMPROP_TRIGGERS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](150) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
	[STR_PROP_1] [nvarchar](512) NULL,
	[STR_PROP_2] [nvarchar](512) NULL,
	[STR_PROP_3] [nvarchar](512) NULL,
	[INT_PROP_1] [int] NULL,
	[INT_PROP_2] [int] NULL,
	[LONG_PROP_1] [bigint] NULL,
	[LONG_PROP_2] [bigint] NULL,
	[DEC_PROP_1] [numeric](13, 4) NULL,
	[DEC_PROP_2] [numeric](13, 4) NULL,
	[BOOL_PROP_1] [bit] NULL,
	[BOOL_PROP_2] [bit] NULL,
	[TIME_ZONE_ID] [nvarchar](80) NULL,
 CONSTRAINT [PK_QRTZ_SIMPROP_TRIGGERS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_TRIGGERS]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](150) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](150) NOT NULL,
	[JOB_NAME] [nvarchar](150) NOT NULL,
	[JOB_GROUP] [nvarchar](150) NOT NULL,
	[DESCRIPTION] [nvarchar](250) NULL,
	[NEXT_FIRE_TIME] [bigint] NULL,
	[PREV_FIRE_TIME] [bigint] NULL,
	[PRIORITY] [int] NULL,
	[TRIGGER_STATE] [nvarchar](16) NOT NULL,
	[TRIGGER_TYPE] [nvarchar](8) NOT NULL,
	[START_TIME] [bigint] NOT NULL,
	[END_TIME] [bigint] NULL,
	[CALENDAR_NAME] [nvarchar](200) NULL,
	[MISFIRE_INSTR] [int] NULL,
	[JOB_DATA] [varbinary](max) NULL,
 CONSTRAINT [PK_QRTZ_TRIGGERS] PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [int] NOT NULL,
	[Name] [varchar](50) NOT NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Trace]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Trace](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[Level] [nvarchar](128) NULL,
	[TimeStamp] [datetimeoffset](7) NOT NULL,
	[Exception] [nvarchar](max) NULL,
	[LogEvent] [nvarchar](max) NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [varchar](50) NOT NULL,
	[Password] [varbinary](128) NOT NULL,
	[Salt] [varbinary](128) NOT NULL,
	[RoleId] [int] NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NULL,
	[EmailAddress1] [nvarchar](250) NULL,
	[EmailAddress2] [nvarchar](250) NULL,
	[EmailAddress3] [nvarchar](250) NULL,
	[PhoneNumber1] [nvarchar](50) NULL,
	[PhoneNumber2] [nvarchar](50) NULL,
	[PhoneNumber3] [nvarchar](50) NULL,
	[Reference1] [nvarchar](500) NULL,
	[Reference2] [nvarchar](500) NULL,
	[Reference3] [nvarchar](500) NULL,
	[Reference4] [nvarchar](500) NULL,
	[Reference5] [nvarchar](500) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_Users] UNIQUE NONCLUSTERED 
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UsersToGroups]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UsersToGroups](
	[UserId] [int] NOT NULL,
	[GroupId] [int] NOT NULL,
 CONSTRAINT [PK_UsersToGroups] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[GroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Groups]    Script Date: 15/01/2023 0:44:07 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Groups] ON [dbo].[Groups]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Groups] ADD  CONSTRAINT [DF_Groups_RoleId]  DEFAULT ((0)) FOR [RoleId]
GO
ALTER TABLE [dbo].[JobInstanceLog] ADD  CONSTRAINT [DF_JobInstanceLog_IsStopped]  DEFAULT ((0)) FOR [IsStopped]
GO
ALTER TABLE [dbo].[MonitorActions] ADD  CONSTRAINT [DF_Monitor_Active]  DEFAULT ((1)) FOR [Active]
GO
ALTER TABLE [dbo].[Groups]  WITH CHECK ADD  CONSTRAINT [FK_Groups_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[Groups] CHECK CONSTRAINT [FK_Groups_Roles]
GO
ALTER TABLE [dbo].[MonitorActions]  WITH CHECK ADD  CONSTRAINT [FK_MonitorActions_Groups] FOREIGN KEY([GroupId])
REFERENCES [dbo].[Groups] ([Id])
GO
ALTER TABLE [dbo].[MonitorActions] CHECK CONSTRAINT [FK_MonitorActions_Groups]
GO
ALTER TABLE [dbo].[QRTZ_CRON_TRIGGERS]  WITH CHECK ADD  CONSTRAINT [FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QRTZ_CRON_TRIGGERS] CHECK CONSTRAINT [FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS]
GO
ALTER TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS]  WITH CHECK ADD  CONSTRAINT [FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS] CHECK CONSTRAINT [FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS]
GO
ALTER TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS]  WITH CHECK ADD  CONSTRAINT [FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS] CHECK CONSTRAINT [FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS]
GO
ALTER TABLE [dbo].[QRTZ_TRIGGERS]  WITH CHECK ADD  CONSTRAINT [FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS] FOREIGN KEY([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
REFERENCES [dbo].[QRTZ_JOB_DETAILS] ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
GO
ALTER TABLE [dbo].[QRTZ_TRIGGERS] CHECK CONSTRAINT [FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS]
GO
ALTER TABLE [dbo].[UsersToGroups]  WITH CHECK ADD  CONSTRAINT [FK_UsersToGroups_Groups] FOREIGN KEY([GroupId])
REFERENCES [dbo].[Groups] ([Id])
GO
ALTER TABLE [dbo].[UsersToGroups] CHECK CONSTRAINT [FK_UsersToGroups_Groups]
GO
ALTER TABLE [dbo].[UsersToGroups]  WITH CHECK ADD  CONSTRAINT [FK_UsersToGroups_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[UsersToGroups] CHECK CONSTRAINT [FK_UsersToGroups_Users]
GO
/****** Object:  StoredProcedure [Admin].[FactoryReset]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Admin].[FactoryReset]
AS
DELETE FROM  [dbo].[QRTZ_BLOB_TRIGGERS]
DELETE FROM  [dbo].[QRTZ_CALENDARS]
DELETE FROM [dbo].[QRTZ_CRON_TRIGGERS]
DELETE FROM [dbo].[QRTZ_FIRED_TRIGGERS]
DELETE FROM [dbo].[QRTZ_LOCKS]
DELETE FROM [dbo].[QRTZ_PAUSED_TRIGGER_GRPS]
DELETE FROM [dbo].[QRTZ_SCHEDULER_STATE]
DELETE FROM [dbo].[QRTZ_SIMPLE_TRIGGERS]
DELETE FROM [dbo].[QRTZ_SIMPROP_TRIGGERS]
DELETE FROM [dbo].[QRTZ_TRIGGERS]
DELETE FROM [dbo].[QRTZ_JOB_DETAILS]

TRUNCATE TABLE [dbo].[ClusterNodes]
TRUNCATE TABLE [dbo].[GlobalConfig]

TRUNCATE TABLE [dbo].[JobInstanceLog]
TRUNCATE TABLE [dbo].[JobProperties]
TRUNCATE TABLE [dbo].[MonitorActions]
TRUNCATE TABLE [dbo].[Trace]
TRUNCATE TABLE [dbo].[UsersToGroups]


DELETE FROM [dbo].[Groups]
DELETE FROM [dbo].[Users]
DELETE FROM   [dbo].[Roles]
GO
/****** Object:  StoredProcedure [Admin].[TestExecPermission]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Tsahi Atias
-- Create date: 2022-12-02
-- Description:	Empty procedure for check permission of execute
-- =============================================
CREATE PROCEDURE [Admin].[TestExecPermission] 
AS
BEGIN
	SELECT 1
END
GO
/****** Object:  StoredProcedure [Admin].[TestPermission]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Tsahi Atias
-- Create date: 2022-12-02
-- Description:	Check the permission of read/write/execute
-- =============================================
CREATE PROCEDURE [Admin].[TestPermission] 
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SomeValue varchar(10) = 'TestWrite'
	BEGIN TRANSACTION

	-- TEST Read
	SELECT TOP 1 [Key] FROM [GlobalConfig]

	-- TEST Write
	INSERT INTO [GlobalConfig] ([Key], [Value], [Type]) VALUES (@SomeValue, @SomeValue, 'string')

	-- TEST Delete
	DELETE FROM [GlobalConfig] WHERE [Key] = @SomeValue

	-- TEST Exec
	EXEC [Admin].[TestExecPermission] 

	ROLLBACK
END
GO
/****** Object:  StoredProcedure [dbo].[ClearTrace]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ClearTrace]
@OverDays int = 365
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @BatchSize INT = 5000
	WHILE 1 = 1
	BEGIN
		DELETE TOP (@BatchSize)
		FROM [dbo].[Trace]
		WHERE DATEDIFF(DAY, [TimeStamp], GETDATE()) > @OverDays
		IF @@ROWCOUNT < @BatchSize BREAK
	END
END
GO
/****** Object:  StoredProcedure [dbo].[CountFailsInHourForJob]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE	[dbo].[CountFailsInHourForJob]
	@JobId varchar(20)
AS
PRINT @JobId
SELECT COUNT([Id])
FROM 
	[dbo].[JobInstanceLog]
WHERE 
	[JobId] = @JobId AND
	[Status] = 1 AND
	DATEDIFF(MINUTE, [EndDate], GETDATE()) <=60
GO
/****** Object:  StoredProcedure [dbo].[CountFailsInRowForJob]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE	[dbo].[CountFailsInRowForJob]
	@JobId varchar(20),
	@Total int
AS
PRINT @JobId
SELECT SUM([Status])
FROM
	(SELECT TOP (@Total) 
		CASE [Status] WHEN 1 THEN 1 ELSE 0 END AS [Status]
	FROM 
		[dbo].[JobInstanceLog]
	WHERE 
		JobId=@JobId
	ORDER BY 
		id DESC) T
GO
/****** Object:  StoredProcedure [dbo].[GetLastHistoryCallForJob]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--exec dbo.GetLastHistoryCallForJob 7

CREATE PROCEDURE [dbo].[GetLastHistoryCallForJob]
 @LastDays int 
AS
WITH added_row_number AS (
  SELECT
       [Id]
      ,[JobId]
      ,[JobName]
      ,[JobGroup]
      ,[TriggerId]
      ,[Status]
      ,[StartDate]
      ,[Duration]
      ,[EffectedRows]
      ,ROW_NUMBER() OVER(PARTITION BY JobId ORDER BY StartDate DESC) AS row_number
  FROM [dbo].[JobInstanceLog]
)
SELECT
  *
FROM added_row_number
WHERE row_number = 1
AND DATEDIFF(day, StartDate, GETDATE())<=@LastDays
ORDER BY StartDate DESC
GO
/****** Object:  StoredProcedure [dbo].[PersistJobInstanceLog]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PersistJobInstanceLog]
	@InstanceId varchar(250),
	@Log nvarchar(max),
	@Exception nvarchar(max),
	@Duration int
AS
	UPDATE [dbo].[JobInstanceLog] SET
	[Log] = @Log,
	[Exception] = @Exception,
	[Duration] = @Duration
WHERE 
	InstanceId = @InstanceId AND Status = -1
GO
/****** Object:  StoredProcedure [dbo].[SetJobInstanceLogStatus]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SetJobInstanceLogStatus]
  @InstanceId varchar(250),
  @Status int,
  @StatusTitle varchar(10)

  AS
  UPDATE [dbo].[JobInstanceLog] SET
	[Status] = @Status,
	[StatusTitle] = @StatusTitle
WHERE 
	InstanceId = @InstanceId
GO
/****** Object:  StoredProcedure [dbo].[UpdateJobInstanceLog]    Script Date: 15/01/2023 0:44:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateJobInstanceLog]
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
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (0, N'anonymous')
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (1, N'viewer')
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (2, N'tester')
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (3, N'editor')
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (4, N'administrator')
GO
