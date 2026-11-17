CREATE TEMP TABLE IF NOT EXISTS temp_raw_audit (
    "RecordId" VARCHAR(100) NOT NULL,
    "TableName" VARCHAR(100) NOT NULL,
    "AuditSiteId" INT NULL,
    "AuditUserId" INT NULL,
    "Reason" TEXT NULL,
    "SysStartTime" TIMESTAMPTZ NOT NULL,
    "SysEndTime" TIMESTAMPTZ NOT NULL,
    "FieldName" VARCHAR(100) NOT NULL,
    "Value" TEXT NULL
) ON COMMIT DROP;

INSERT INTO temp_raw_audit ("RecordId", "TableName", "AuditSiteId", "AuditUserId", "Reason", "SysStartTime", "SysEndTime", "FieldName", "Value")
{UnpivotedTables};

CREATE INDEX IF NOT EXISTS idx_temp_raw_audit_perf ON temp_raw_audit ("TableName", "RecordId");

WITH Unpivoted AS
(
    SELECT "RecordId", "TableName", "AuditSiteId", "AuditUserId", "Reason", "SysStartTime", "SysEndTime", "FieldName", "Value"
    FROM temp_raw_audit
),
MetaData AS
(
    {MetaDataTables}
),
Changes AS
(
    SELECT *,
           LAG("Value") OVER (PARTITION BY "RecordId", "TableName", "FieldName" ORDER BY "SysStartTime") AS "OldValue"
    FROM Unpivoted
)
SELECT 
    FinalAudit."RecordId",
    FinalAudit."ActionType",
    FinalAudit."TableName",
    FinalAudit."SiteId",
    FinalAudit."FieldName",
    FinalAudit."UserId",
    FinalAudit."Timestamp",
    FinalAudit."NewValue",
    FinalAudit."OldValue",
    FinalAudit."Reason" 
FROM (
    SELECT
        chg."RecordId",
        CASE 
            WHEN chg."SysStartTime" = meta."CreateTime" THEN 'Create'
            ELSE 'Update'
        END AS "ActionType",
        chg."TableName",
        chg."AuditSiteId" AS "SiteId",
        chg."FieldName",
        chg."AuditUserId" AS "UserId",
        chg."SysStartTime" AS "Timestamp",
        chg."Value" AS "NewValue",
        chg."OldValue" AS "OldValue",
        chg."Reason"
    FROM Changes chg
    LEFT JOIN MetaData meta 
        ON chg."RecordId" = meta."RecordId" 
       AND chg."TableName" = meta."TableName"
    WHERE (chg."OldValue" IS NULL OR chg."OldValue" <> chg."Value") 

    UNION ALL

    SELECT DISTINCT ON (chg."RecordId", chg."TableName", chg."FieldName")
        chg."RecordId",
        'Delete' AS "ActionType",
        chg."TableName",
        chg."AuditSiteId" AS "SiteId",
        chg."FieldName",
        chg."AuditUserId" AS "UserId",
        chg."SysEndTime" AS "Timestamp",
        NULL AS "NewValue",
        chg."Value" AS "OldValue",
        chg."Reason"
    FROM Changes chg
    LEFT JOIN MetaData meta 
        ON chg."RecordId" = meta."RecordId" 
       AND chg."TableName" = meta."TableName"
    WHERE chg."SysEndTime" = meta."EndTime" 
      AND meta."EndTime" < '9999-12-31'::timestamptz
    ORDER BY "RecordId", "TableName", "FieldName", "Timestamp" DESC
) FinalAudit
{TemporalClause}
ORDER BY FinalAudit."Timestamp", FinalAudit."TableName", FinalAudit."NewValue", FinalAudit."OldValue";