SELECT
    unpvt.RecordId,
    unpvt.TableName,
    unpvt.AuditSiteId,
    unpvt.AuditUserId,
    unpvt.[Reason],
    unpvt.SysStartTime,
    unpvt.SysEndTime,
    unpvt.AuditFieldName AS FieldName,
    unpvt.AuditFieldValue AS Value
FROM
(
    SELECT 
        CAST(t.[{RecordIdColumnName}] AS VARCHAR(100)) AS RecordId,
        '{TableName}' AS TableName,
        {Columns},
        {SiteIdExpression} AS AuditSiteId,
        t.[LastModifiedBy] AS AuditUserId,
        {ReasonColumn}
        t.[SysStartTime],
        t.[SysEndTime]
    FROM [{Schema}].[{TableName}]
    {TemporalClause} AS t
    {SiteJoin}
    {WhereClause}
) AS src
UNPIVOT
(
    AuditFieldValue FOR AuditFieldName IN ({UnpivotFields})
) AS unpvt