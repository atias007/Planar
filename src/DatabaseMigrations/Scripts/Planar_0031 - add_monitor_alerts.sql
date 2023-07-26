CREATE TABLE [dbo].[MonitorAlerts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MonitorId] [int] NOT NULL,
	[MonitorTitle] [nvarchar](50) NOT NULL,
	[EventId] [int] NOT NULL,
	[EventTitle] [varchar](50) NOT NULL,
	[EventArgument] [varchar](50) NULL,
	[JobName] [varchar](50) NULL,
	[JobGroup] [varchar](50) NULL,
	[JobId] [varchar](20) NULL,
	[GroupId] [int] NOT NULL,
	[GroupName] [nvarchar](50) NOT NULL,
	[UsersCount] [int] NOT NULL,
	[Hook] [varchar](50) NOT NULL,
	[LogInstanceId] [varchar](250) NULL,
	[HasError] [bit] NOT NULL,
	[AlertDate] [datetime] NOT NULL,
	[Exception] [nvarchar](max) NULL,
	[AlertPayload] [nvarchar](max) NULL,
 CONSTRAINT [PK_MonitorAlerts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))