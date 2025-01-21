DROP TABLE IF EXISTS temp_JobProperties;
CREATE TABLE temp_JobProperties (JobId TEXT, Properties TEXT);
INSERT INTO temp_JobProperties SELECT JobId, Properties FROM JobProperties;
DROP TABLE JobProperties;
CREATE TABLE [JobProperties]([JobId] [varchar](20) NOT NULL,[Properties] [nvarchar] NULL,[JobType] varchar(100) NOT NULL, PRIMARY KEY ([JobId]));
INSERT INTO JobProperties SELECT JobId, Properties, 'Unknown' FROM temp_JobProperties;
DROP TABLE IF EXISTS temp_JobProperties;
