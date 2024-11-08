--BEGIN TRANSACTION;

DROP TABLE IF EXISTS CTE;

CREATE TEMPORARY TABLE CTE
AS
SELECT 
	date(RecordDate) AS [RecordDate],
	strftime('%H', RecordDate) AS [RecordHour],
	[Server],
	[InstanceId],
	[ConcurrentValue]
  FROM [ConcurrentQueue]
  WHERE RecordDate < @current;


SELECT * FROM CTE;

INSERT INTO [ConcurrentExecution]
           ([RecordDate]
           ,[Server]
           ,[InstanceId]
           ,[MaxConcurrent])
SELECT 
	datetime(strftime('%Y-%m-%d %H:%M:%S', [RecordDate], [RecordHour] || ' hour')) AS [RecordDate],
	[Server],
	[InstanceId],
	MAX([ConcurrentValue]) [ConcurrentValue]
FROM CTE
GROUP BY
	[RecordDate],
	[RecordHour],
	[Server],
	[InstanceId];

DELETE FROM [ConcurrentQueue]
WHERE RecordDate < @current;


-- COMMIT;