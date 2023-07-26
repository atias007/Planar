CREATE TABLE [dbo].[MonitorAlerts] (
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [MonitorId]     INT            NOT NULL,
    [MonitorTitle]  NVARCHAR (50)  NOT NULL,
    [EventId]       INT            NOT NULL,
    [EventTitle]    VARCHAR (50)   NOT NULL,
    [EventArgument] VARCHAR (50)   NULL,
    [JobName]       VARCHAR (50)   NULL,
    [JobGroup]      VARCHAR (50)   NULL,
    [JobId]         VARCHAR (20)   NULL,
    [GroupId]       INT            NOT NULL,
    [GroupName]     NVARCHAR (50)  NOT NULL,
    [UsersCount]    INT            NOT NULL,
    [Hook]          VARCHAR (50)   NOT NULL,
    [LogInstanceId] VARCHAR (250)  NULL,
    [HasError]      BIT            NOT NULL,
    [AlertDate]     DATETIME       NOT NULL,
    [Exception]     NVARCHAR (MAX) NULL,
    [AlertPayload]  NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_MonitorAlerts] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

