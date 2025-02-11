
  ALTER TABLE [dbo].[JobProperties] ADD [JobType] varchar(100)
  GO

  UPDATE [dbo].[JobProperties] SET [JobType] = 'Unknown'
  GO

  ALTER TABLE [dbo].[JobProperties] ALTER COLUMN [JobType] varchar(100) NOT NULL
