SELECT
    FinalAudit.RecordId,
    'Create' AS ActionType,
    FinalAudit.TableName,
    FinalAudit.AuditSiteId AS SiteId,
    FinalAudit.FieldName,
    FinalAudit.AuditUserId AS UserId,
    FinalAudit.AuditTimestamp AS [Timestamp],
    FinalAudit.Value AS NewValue,
    NULL AS OldValue,
    FinalAudit.Reason
FROM
(
    {UnpivotedTables}
) AS FinalAudit
{TemporalClause}
ORDER BY FinalAudit.AuditTimestamp, FinalAudit.TableName, FinalAudit.Value;
