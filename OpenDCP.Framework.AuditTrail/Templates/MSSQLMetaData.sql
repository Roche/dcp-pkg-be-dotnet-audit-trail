SELECT 
       CAST(t.[{RecordIdColumnName}] AS VARCHAR(100)) AS RecordId,
       '{TableName}' AS TableName,
       MIN(t.[SysStartTime]) AS CreateTime,
       MAX(t.[SysEndTime]) AS EndTime
FROM [{Schema}].[{TableName}] FOR SYSTEM_TIME ALL AS t
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT DISTINCT RecordId
        FROM Unpivoted
        WHERE TableName = '{TableName}'
    ) AS u
    WHERE {RecordIdPredicate}
)
GROUP BY t.[{RecordIdColumnName}]