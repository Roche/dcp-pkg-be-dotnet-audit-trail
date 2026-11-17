SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#RawAudit') IS NOT NULL DROP TABLE #RawAudit;

CREATE TABLE #RawAudit (
    RecordId VARCHAR(100) NOT NULL,
    TableName VARCHAR(100) NOT NULL,
    AuditSiteId INT NULL,
    AuditUserId INT NULL,
    [Reason] VARCHAR(MAX) NULL,
    SysStartTime DATETIME2(7) NOT NULL,
    SysEndTime DATETIME2(7) NOT NULL,
    FieldName VARCHAR(100) NOT NULL,
    Value VARCHAR(MAX) NULL
);

INSERT INTO #RawAudit (RecordId, TableName, AuditSiteId, AuditUserId, [Reason], SysStartTime, SysEndTime, FieldName, Value)
{UnpivotedTables}

CREATE CLUSTERED INDEX IX_RawAudit_Performance_Seek ON #RawAudit (TableName, RecordId);

WITH Unpivoted AS
(
    SELECT RecordId, TableName, AuditSiteId, AuditUserId, [Reason], SysStartTime, SysEndTime, FieldName, Value
    FROM #RawAudit
),
MetaData AS
(
    {MetaDataTables}
),
Changes AS
(
    SELECT *,
           LAG(Value) OVER (PARTITION BY RecordId, TableName, FieldName ORDER BY SysStartTime) AS OldValue
    FROM Unpivoted
)
SELECT 
    FinalAudit.RecordId,
    FinalAudit.ActionType,
    FinalAudit.TableName,
    FinalAudit.SiteId,
    FinalAudit.FieldName,
    FinalAudit.UserId,
    FinalAudit.[Timestamp],
    FinalAudit.NewValue,
    FinalAudit.OldValue,
    FinalAudit.Reason
FROM (
    SELECT
        chg.RecordId,
        CASE 
            WHEN chg.SysStartTime = meta.CreateTime THEN 'Create'
            ELSE 'Update'
        END AS ActionType,
        chg.TableName,
        chg.AuditSiteId AS SiteId,
        chg.FieldName,
        chg.AuditUserId AS UserId,
        chg.SysStartTime AS [Timestamp],
        chg.Value AS NewValue,
        chg.OldValue AS OldValue,
        chg.Reason
    FROM Changes chg
    LEFT JOIN MetaData meta 
        ON chg.RecordId = meta.RecordId 
       AND chg.TableName = meta.TableName
    WHERE (chg.OldValue IS NULL OR chg.OldValue <> chg.Value)

    UNION ALL

    SELECT 
        Deduplicated.RecordId,
        Deduplicated.ActionType,
        Deduplicated.TableName,
        Deduplicated.SiteId,
        Deduplicated.FieldName,
        Deduplicated.UserId,
        Deduplicated.[Timestamp],
        Deduplicated.NewValue,
        Deduplicated.OldValue,
        Deduplicated.Reason
    FROM (
        SELECT
            chg.RecordId,
            'Delete' AS ActionType,
            chg.TableName,
            chg.AuditSiteId AS SiteId,
            chg.FieldName,
            chg.AuditUserId AS UserId,
            chg.SysEndTime AS [Timestamp],
            NULL AS NewValue,
            chg.Value AS OldValue,
            chg.Reason,
            ROW_NUMBER() OVER (
                PARTITION BY chg.RecordId, chg.TableName, chg.FieldName 
                ORDER BY chg.SysEndTime DESC
            ) AS RowNum
        FROM Changes chg
        LEFT JOIN MetaData meta 
            ON chg.RecordId = meta.RecordId 
           AND chg.TableName = meta.TableName
        WHERE chg.SysEndTime = meta.EndTime 
          AND meta.EndTime < '9999-12-31 23:59:59.9999999'
    ) Deduplicated
    WHERE Deduplicated.RowNum = 1
) FinalAudit
{TemporalClause}
ORDER BY FinalAudit.[Timestamp], FinalAudit.TableName, FinalAudit.NewValue, FinalAudit.OldValue;

IF OBJECT_ID('tempdb..#RawAudit') IS NOT NULL DROP TABLE #RawAudit;