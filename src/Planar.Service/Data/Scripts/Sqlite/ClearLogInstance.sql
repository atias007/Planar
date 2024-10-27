DELETE FROM JobInstanceLog
WHERE julianday('now', 'localtime') - julianday(StartDate, 'localtime') > @OverDays