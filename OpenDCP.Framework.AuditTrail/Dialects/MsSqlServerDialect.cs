using OpenDCP.Framework.AuditTrail.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenDCP.Framework.AuditTrail.Dialects
{
    public sealed class MsSqlServerDialect : SqlDialectBase
    {
        public override string TemplatePrefix => "MSSQL";

        private const string CastType = "VARCHAR(MAX)";

        public override string BuildColumns(IEnumerable<(string Name, Type PropertyType)> columnsToAudit, string dateTimePattern)
        {
            return string.Join(", ", columnsToAudit.Select(c =>
            {
                bool isDateTime = c.PropertyType == typeof(DateTime) ||
                                  c.PropertyType == typeof(DateTime?) ||
                                  c.PropertyType == typeof(DateTimeOffset) ||
                                  c.PropertyType == typeof(DateTimeOffset?);

                return isDateTime
                    ? $"CAST(FORMAT(t.[{c.Name}], '{dateTimePattern}') AS {CastType}) AS [{c.Name}]"
                    : $"CAST(t.[{c.Name}] AS {CastType}) AS [{c.Name}]";
            }));
        }

        public override string BuildUnpivotFields(IEnumerable<(string Name, Type PropertyType)> columnsToAudit)
        {
            return string.Join(", ", columnsToAudit
                .Select(c => $"[{c.Name}]"));
        }

        public override string BuildSiteIdExpression(AuditTableConfig cfg)
        {
            return cfg.SiteIdColumn.Contains('.')
                ? cfg.SiteIdColumn
                : $"t.[{cfg.SiteIdColumn}]";
        }

        public override string BuildReasonColumn(bool hasReason)
        {
            return hasReason
                ? "t.[Reason] AS [Reason],"
                : $"CAST(NULL AS {CastType}) AS [Reason],";
        }

        public override string BuildWhereClause(AuditTableConfig cfg, object recordId, int? siteId,
            DateTime? startTime, DateTime? endTime)
        {
            var whereFilters = new List<string>();

            if (startTime is not null && endTime is not null)
            {
                whereFilters.Add($"(SysStartTime BETWEEN '{startTime:O}' AND '{endTime:O}' OR SysEndTime BETWEEN '{startTime:O}' AND '{endTime:O}')");
            }

            if (recordId != null)
            {
                bool isNumeric = recordId is int or long or short;

                if (!string.IsNullOrEmpty(cfg.RelationFilterSql))
                {
                    var filterSql = cfg.RelationFilterSql.Replace("{RecordId}", recordId.ToString());
                    whereFilters.Add(filterSql);
                }
                else
                {
                    var recordColumn = $"t.[{cfg.RecordIdColumn}]";

                    whereFilters.Add(isNumeric
                        ? $"{recordColumn} = {recordId}"
                        : $"{recordColumn} = '{recordId}'");
                }
            }
            else if (siteId != null)
            {
                var targetColumn = cfg.SiteIdColumn.Contains('.')
                    ? cfg.SiteIdColumn
                    : $"t.[{cfg.SiteIdColumn}]";

                whereFilters.Add($"{targetColumn} = {siteId}");
            }

            return whereFilters.Count > 0
                ? "WHERE " + string.Join(" AND ", whereFilters)
                : string.Empty;
        }

        public override string BuildInnerTemporalClause(DateTime? start, DateTime? end)
        {
            return start is null || end is null
                ? "FOR SYSTEM_TIME ALL"
                : $"FOR SYSTEM_TIME BETWEEN '{start:O}' AND '{end:O}'";
        }

        public override string BuildOuterTemporalFilter(DateTime? start, DateTime? end)
        {
            if (start == null || end == null)
            {
                return string.Empty;
            }

            return $" WHERE FinalAudit.[Timestamp] BETWEEN '{start:O}' AND '{end:O}'";
        }
        public override string BuildMetaDataRecordIdPredicate(AuditTableConfig cfg)
        {
            var recordIdColumn = $"t.[{cfg.RecordIdColumn}]";
            var recordIdValue = "u.RecordId";

            var sqlType = GetMsSqlRecordIdConvertType(cfg.RecordIdClrType, cfg.RecordIdColumnType);

            if (sqlType == null)
            {
                // String or unsupported type.
                // Do not cast the table column.
                return $"{recordIdColumn} = {recordIdValue}";
            }

            return $"{recordIdColumn} = TRY_CONVERT({sqlType}, {recordIdValue})";
        }

        private static string GetMsSqlRecordIdConvertType(Type clrType, string columnType)
        {
            clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;

            if (clrType == typeof(int))
            {
                return "INT";
            }

            if (clrType == typeof(long))
            {
                return "BIGINT";
            }

            if (clrType == typeof(short))
            {
                return "SMALLINT";
            }

            if (clrType == typeof(byte))
            {
                return "TINYINT";
            }

            if (clrType == typeof(Guid))
            {
                return "UNIQUEIDENTIFIER";
            }

            if (clrType == typeof(string))
            {
                return null;
            }

            // Fallback: if EF gives us a SQL column type, we can use it.
            // Example: decimal(18,0), numeric(18,0), uniqueidentifier, bigint, etc.
            if (!string.IsNullOrWhiteSpace(columnType))
            {
                return columnType;
            }

            return null;
        }
    }
}
