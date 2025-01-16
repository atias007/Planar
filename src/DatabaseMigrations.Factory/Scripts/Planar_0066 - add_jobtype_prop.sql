  IF NOT EXISTS (
  SELECT *
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'JobProperties'
      AND COLUMN_NAME = 'JobType')
BEGIN
  ALTER TABLE [dbo].[JobProperties] ADD [JobType] varchar(100) NULL;
  UPDATE [dbo].[JobProperties] SET [JobType] = 'Unknown';
  ALTER TABLE [dbo].[JobProperties] ALTER COLUMN [JobType] varchar(100) NOT NULL;
END
