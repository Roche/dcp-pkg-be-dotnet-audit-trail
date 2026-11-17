SELECT
    src."RecordId",
    src."TableName",
    src."AuditSiteId",
    src."AuditUserId",
    src."SysStartTime",
    src."SysEndTime",
    src."Reason",
    kv.key AS "FieldName",
    kv.value AS "Value"
FROM (
    SELECT
        CAST(t."{RecordIdColumnName}" AS VARCHAR(100)) AS "RecordId",
        '{TableName}' AS "TableName",
        {SiteIdExpression} AS "AuditSiteId",
        t."LastModifiedBy" AS "AuditUserId",
        t."SysStartTime" AS "SysStartTime",
        t."SysEndTime" AS "SysEndTime",
        {ReasonColumn}
        to_jsonb(t)
            - '{RecordIdColumnName}'
            - 'SysStartTime'
            - 'SysEndTime'
            - 'LastModifiedBy'
            - 'OwnerUserId'
            - 'Reason' AS json_columns
    FROM {Schema}."{TableName}__{TemporalClause} AS t
    {SiteJoin}
    {WhereClause}
) AS src
CROSS JOIN LATERAL jsonb_each_text(src.json_columns) AS kv(key, value)