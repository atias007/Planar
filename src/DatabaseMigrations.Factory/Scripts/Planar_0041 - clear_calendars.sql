UPDATE [dbo].[QRTZ_TRIGGERS] SET CALENDAR_NAME=NULL WHERE CALENDAR_NAME IS NOT NULL
GO
TRUNCATE TABLE [dbo].[QRTZ_CALENDARS]
