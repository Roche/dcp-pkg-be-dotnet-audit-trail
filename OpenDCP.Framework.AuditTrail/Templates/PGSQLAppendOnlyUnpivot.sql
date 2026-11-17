SELECT
    unpvt."RecordId",
    unpvt."TableName",
    unpvt."AuditSiteId",
    unpvt."AuditUserId",
    unpvt."Reason",
    unpvt."AuditTimestamp",
    unpvt."AuditFieldName" AS "FieldName",
    unpvt."AuditFieldValue" AS "Value"
FROM
(
    SELECT 
        CAST(t."{RecordIdColumnName}" AS VARCHAR(100)) AS "RecordId",
        '{TableName}' AS "TableName",
        {SiteIdExpression} AS "AuditSiteId",
        {UserIdExpression} AS "AuditUserId",
        {ReasonColumn}
        {TimestampExpression} AS "AuditTimestamp",
        unpvt_fields."AuditFieldName",
        unpvt_fields."AuditFieldValue"
    FROM "{Schema}"."{TableName}" AS t
    {SiteJoin}
    CROSS JOIN LATERAL (
        VALUES {UnpivotValuesList}
    ) AS unpvt_fields("AuditFieldName", "AuditFieldValue")
    {WhereClause}
) AS unpvt
