SELECT
    unpvt.RecordId,
    unpvt.TableName,
    unpvt.AuditSiteId,
    unpvt.AuditUserId,
    unpvt.[Reason],
    unpvt.AuditTimestamp,
    unpvt.AuditFieldName AS FieldName,
    unpvt.AuditFieldValue AS Value
FROM
(
    SELECT 
        CAST(t.[{RecordIdColumnName}] AS VARCHAR(100)) AS RecordId,
        '{TableName}' AS TableName,
        {Columns},
        {SiteIdExpression} AS AuditSiteId,
        {UserIdExpression} AS AuditUserId,
        {ReasonColumn}
        {TimestampExpression} AS AuditTimestamp
    FROM [{Schema}].[{TableName}] AS t
    {SiteJoin}
    {WhereClause}
) AS src
UNPIVOT
(
    AuditFieldValue FOR AuditFieldName IN ({UnpivotFields})
) AS unpvt
