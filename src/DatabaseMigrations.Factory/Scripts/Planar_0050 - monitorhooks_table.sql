CREATE TABLE [dbo].[MonitorHooks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](2000) NOT NULL,
	[Path] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_MonitorHooks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)
)
GO


CREATE UNIQUE NONCLUSTERED INDEX IX_MonitorHooks ON dbo.MonitorHooks
	(
	Name
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO