using OpenDCP.Framework.AuditTrail.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenDCP.Framework.AuditTrail.Dialects
{
    public sealed class PostgreSqlDialect : SqlDialectBase
    {
        public override string TemplatePrefix => "PGSQL";

        private const string CastType = "TEXT";

        public override string BuildColumns(IEnumerable<(string Name, Type PropertyType)> columnsToAudit, string dateTimePattern)
        {
            string pgPattern = dateTimePattern
                .Replace("yyyy", "YYYY")
                .Replace("dd", "DD")
                .Replace("HH", "HH24")
                .Replace("hh", "HH12")
                .Replace("mm", "MI")
                .Replace("ss", "SS")
                .Replace("fff", "MS")
                .Replace("tt", "AM")
                .Replace("t", "AM")
                .Replace("'", "");

            return string.Join(", ", columnsToAudit.Select(c =>
            {
                bool isDateTime = c.PropertyType == typeof(DateTime) ||
                                  c.PropertyType == typeof(DateTime?) ||
                                  c.PropertyType == typeof(DateTimeOffset) ||
                                  c.PropertyType == typeof(DateTimeOffset?);

                return isDateTime
                    ? $"CAST(to_char(t.\"{c.Name}\", '{pgPattern}') AS {CastType}) AS \"{c.Name}\""
                    : $"CAST(t.\"{c.Name}\" AS {CastType}) AS \"{c.Name}\"";
            }));
        }

        public override string BuildUnpivotFields(IEnumerable<(string Name, Type PropertyType)> columnsToAudit)
        {
            return string.Join(", ", columnsToAudit
               .Select(c => $"\"{c.Name}\""));
        }

        public override string BuildSiteIdExpression(AuditTableConfig cfg)
        {
            return cfg.SiteIdColumn.Contains('.')
               ? cfg.SiteIdColumn
               : cfg.SiteIdColumn;
        }

        public override string BuildReasonColumn(bool hasReason)
        {
            return hasReason
                 ? "t.\"Reason\" AS \"Reason\","
                 : "CAST(NULL AS TEXT) AS \"Reason\",";
        }

        public override string BuildWhereClause(AuditTableConfig cfg, object recordId, int? siteId,
            DateTime? startTime, DateTime? endTime)
        {
            var whereFilters = new List<string>();

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
                    var recordColumn = $"t.\"{cfg.RecordIdColumn}\"";

                    whereFilters.Add(isNumeric
                        ? $"{recordColumn} = {recordId}"
                        : $"{recordColumn} = '{recordId}'");
                }
            }
            else if (siteId != null)
            {
                var targetColumn = cfg.SiteIdColumn.Contains('.')
                    ? cfg.SiteIdColumn
                    : $"t.\"{cfg.SiteIdColumn}\"";

                whereFilters.Add($"{targetColumn} = {siteId}");
            }

            if (startTime is not null && endTime is not null)
            {
                whereFilters.Add($"(\"SysStartTime\" BETWEEN '{startTime:O}'::timestamptz AND '{endTime:O}'::timestamptz OR \"SysEndTime\" BETWEEN '{startTime:O}'::timestamptz AND '{endTime:O}'::timestamptz)");
            }

            return whereFilters.Count > 0
                ? "WHERE " + string.Join(" AND ", whereFilters)
                : string.Empty;
        }

        public override string BuildInnerTemporalClause(DateTime? start, DateTime? end)
        {
            return start is null || end is null
              ? "from_to\"('-infinity'::timestamptz,'infinity'::timestamptz)"
              : $"from_to\"('{start:O}'::timestamptz,'{end:O}'::timestamptz)";
        }

        public override string BuildOuterTemporalFilter(DateTime? start, DateTime? end)
        {
            if (start == null || end == null)
            {
                return string.Empty;
            }

            return $" WHERE FinalAudit.\"Timestamp\" BETWEEN '{start:O}'::timestamptz AND '{end:O}'::timestamptz";
        }

        public override string BuildMetaDataRecordIdPredicate(AuditTableConfig cfg)
        {
            return $"CAST(t.\"{cfg.RecordIdColumn}\" AS VARCHAR(100)) = u.\"RecordId\"";
        }
    }
}
