SELECT 
       CAST(t."{RecordIdColumnName}" AS VARCHAR(100)) AS "RecordId",
       '{TableName}' AS "TableName",
       MIN(t."SysStartTime") AS "CreateTime",
       MAX(t."SysEndTime") AS "EndTime"
FROM {Schema}."{TableName}__from_to"('-infinity','infinity') AS t
WHERE CAST(t."{RecordIdColumnName}" AS VARCHAR(100)) IN (
    SELECT DISTINCT "RecordId" 
    FROM Unpivoted 
    WHERE "TableName" = '{TableName}'
)
GROUP BY t."{RecordIdColumnName}"