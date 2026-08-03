ALTER TABLE QRTZ_TRIGGERS ADD COLUMN EXECUTION_GROUP NVARCHAR(200);
ALTER TABLE QRTZ_FIRED_TRIGGERS ADD COLUMN EXECUTION_GROUP NVARCHAR(200);
ALTER TABLE QRTZ_TRIGGERS ADD COLUMN PREFERRED_NODE NVARCHAR(200);
ALTER TABLE QRTZ_TRIGGERS ADD COLUMN PREFERRED_NODE_AUTO BIT NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS IDX_QRTZ_J_G_N ON QRTZ_JOB_DETAILS(SCHED_NAME,JOB_GROUP,JOB_NAME);
CREATE INDEX IF NOT EXISTS IDX_QRTZ_T_G_N ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_GROUP,TRIGGER_NAME);
-- Serves SelectTriggersForJob, SelectNumTriggersForJob, both UpdateJobTriggerStates statements and the trigger listing's job filter.
CREATE INDEX IF NOT EXISTS IDX_QRTZ_T_J ON QRTZ_TRIGGERS(SCHED_NAME,JOB_NAME,JOB_GROUP);

-- Serves SelectTriggersForCalendar and SelectReferencedCalendar, which otherwise scan every trigger on each calendar store and remove.
CREATE INDEX IF NOT EXISTS IDX_QRTZ_T_C ON QRTZ_TRIGGERS(SCHED_NAME,CALENDAR_NAME);

-- Serves trigger acquisition, the misfire count and the misfire recovery select, which run on every scheduler poll.
CREATE INDEX IF NOT EXISTS IDX_QRTZ_T_NFT_ST ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_STATE,NEXT_FIRE_TIME);

-- Serves SelectInstancesRecoverableFiredTriggers, the instance-name filter of the fired-trigger select and delete, and SelectFiredTriggerInstanceNames.
CREATE INDEX IF NOT EXISTS IDX_QRTZ_FT_INST_JOB_REQ_RCVRY ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,INSTANCE_NAME,REQUESTS_RECOVERY);

-- Serves the job filter of the fired-trigger select and delete, and IsJobCurrentlyExecuting, which runs on every fire of a non-concurrent job.
CREATE INDEX IF NOT EXISTS IDX_QRTZ_FT_J_G ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,JOB_NAME,JOB_GROUP);

-- Serves the trigger filter of the fired-trigger select and delete, and IsTriggerCurrentlyExecuting.
CREATE INDEX IF NOT EXISTS IDX_QRTZ_FT_T_G ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP);