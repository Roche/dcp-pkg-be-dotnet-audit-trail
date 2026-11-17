using Microsoft.EntityFrameworkCore;
using OpenDCP.Framework.AuditTrail.Attributes;
using OpenDCP.Framework.AuditTrail.Factory;
using OpenDCP.Framework.AuditTrail.Interfaces;
using OpenDCP.Framework.AuditTrail.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenDCP.Framework.AuditTrail.Classes
{
    public class AuditHelper
    {
        private readonly DbContext _context;
        private readonly IDialects _dialect;
        public bool IncludeId { get; set; } = false;
        public string DateTimePattern { get; set; } = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public bool SiteIdAsColumn { get; set; } = true;

        public AuditHelper(DbContext context)
        {
            _context = context;
            _dialect = DialectFactory.Create(context);
        }

        public async Task<List<AuditEntryDTO>> GetAuditForTablesAsync(
            DateTime? startTime,
            DateTime? endTime,
            object recordId = null,
            int? siteId = null,
            List<Type> tables = null)
        {
            if (recordId != null && siteId != null)
            {
                throw new ArgumentException("Cannot filter by both RecordId and SiteId simultaneously. Provide only one.");
            }

            var auditEntities = GetAuditTableConfigurations(tables);

            var systemVersionedConfigs = auditEntities.Where(c => c.IsSystemVersioned).ToList();
            var appendOnlyConfigs = auditEntities.Where(c => !c.IsSystemVersioned).ToList();

            var results = new List<AuditEntryDTO>();

            if (systemVersionedConfigs.Count > 0)
            {
                results.AddRange(await GetSystemVersionedAuditAsync(systemVersionedConfigs, recordId, siteId, startTime, endTime));
            }

            if (appendOnlyConfigs.Count > 0)
            {
                results.AddRange(await GetAppendOnlyAuditAsync(appendOnlyConfigs, recordId, siteId, startTime, endTime));
            }

            return results
                .OrderBy(r => r.Timestamp)
                .ThenBy(r => r.TableName)
                .ThenBy(r => r.NewValue)
                .ThenBy(r => r.OldValue)
                .ToList();
        }

        private async Task<List<AuditEntryDTO>> GetSystemVersionedAuditAsync(
            List<AuditTableConfig> auditEntities,
            object recordId,
            int? siteId,
            DateTime? startTime,
            DateTime? endTime)
        {
            var metaDataTemplate = _dialect.LoadTemplate("MetaData.sql", LoadTemplate);
            var unpivotTemplate = _dialect.LoadTemplate("Unpivot.sql", LoadTemplate);
            var auditTemplate = _dialect.LoadTemplate("AuditTrailQuery.sql", LoadTemplate);

            var unpivotSnippets = new List<string>();
            var metaSnippets = new List<string>();

            foreach (var cfg in auditEntities)
            {
                var columns = _dialect.BuildColumns(cfg.ColumnsToAudit, DateTimePattern);
                var unpivotFields = _dialect.BuildUnpivotFields(cfg.ColumnsToAudit);
                var siteIdExpression = _dialect.BuildSiteIdExpression(cfg);
                var reasonColumn = _dialect.BuildReasonColumn(cfg.HasReason);
                var whereClause = _dialect.BuildWhereClause(cfg, recordId, siteId, startTime, endTime);
                var temporalClause = _dialect.BuildInnerTemporalClause(startTime, endTime);
                var recordIdPredicate = _dialect.BuildMetaDataRecordIdPredicate(cfg);

                unpivotSnippets.Add(
                    unpivotTemplate
                        .Replace("{TableName}", cfg.TableName)
                        .Replace("{RecordIdColumnName}", cfg.RecordIdColumn)
                        .Replace("{Schema}", cfg.Schema)
                        .Replace("{Columns}", columns)
                        .Replace("{SiteIdExpression}", siteIdExpression)
                        .Replace("{SiteJoin}", cfg.SiteJoin ?? string.Empty)
                        .Replace("{WhereClause}", whereClause)
                        .Replace("{TemporalClause}", temporalClause)
                        .Replace("{UnpivotFields}", unpivotFields)
                        .Replace("{ReasonColumn}", reasonColumn));

                metaSnippets.Add(metaDataTemplate
                        .Replace("{TableName}", cfg.TableName)
                        .Replace("{RecordIdColumnName}", cfg.RecordIdColumn)
                        .Replace("{Schema}", cfg.Schema)
                        .Replace("{RecordIdPredicate}", recordIdPredicate));
            }

            var outerTemporalFilter = _dialect.BuildOuterTemporalFilter(startTime, endTime);

            var finalSql = auditTemplate
                .Replace("{UnpivotedTables}", string.Join(" UNION ALL ", unpivotSnippets))
                .Replace("{MetaDataTables}", string.Join(" UNION ALL ", metaSnippets))
                .Replace("{TemporalClause}", outerTemporalFilter);

            return await _context.Set<AuditEntryDTO>()
                .FromSqlRaw(finalSql)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<AuditEntryDTO>> GetAppendOnlyAuditAsync(
            List<AuditTableConfig> auditEntities,
            object recordId,
            int? siteId,
            DateTime? startTime,
            DateTime? endTime)
        {
            var unpivotTemplate = _dialect.LoadTemplate("AppendOnlyUnpivot.sql", LoadTemplate);
            var auditTemplate = _dialect.LoadTemplate("AppendOnlyAuditTrailQuery.sql", LoadTemplate);

            var unpivotSnippets = new List<string>();

            foreach (var cfg in auditEntities)
            {
                var siteIdExpression = _dialect.BuildSiteIdExpression(cfg);
                var reasonColumn = _dialect.BuildReasonColumn(cfg.HasReason);
                var whereClause = _dialect.BuildWhereClause(cfg, recordId, siteId, null, null);
                var columns = _dialect.BuildColumns(cfg.ColumnsToAudit, DateTimePattern);
                var unpivotFields = _dialect.BuildUnpivotFields(cfg.ColumnsToAudit);

                unpivotSnippets.Add(unpivotTemplate
                    .Replace("{TableName}", cfg.TableName)
                    .Replace("{RecordIdColumnName}", cfg.RecordIdColumn)
                    .Replace("{Schema}", cfg.Schema)
                    .Replace("{Columns}", columns)
                    .Replace("{SiteIdExpression}", siteIdExpression)
                    .Replace("{UserIdExpression}", cfg.UserIdColumnExpression)
                    .Replace("{ReasonColumn}", reasonColumn)
                    .Replace("{TimestampExpression}", cfg.TimestampExpression)
                    .Replace("{UnpivotFields}", unpivotFields)
                    .Replace("{SiteJoin}", cfg.SiteJoin ?? string.Empty)
                    .Replace("{WhereClause}", whereClause));
            }

            var outerTemporalFilter = _dialect.BuildOuterTemporalFilter(startTime, endTime);

            var finalSql = auditTemplate
                .Replace("{UnpivotedTables}", string.Join(" UNION ALL ", unpivotSnippets))
                .Replace("{TemporalClause}", outerTemporalFilter);

            return await _context.Set<AuditEntryDTO>()
                .FromSqlRaw(finalSql)
                .AsNoTracking()
                .ToListAsync();
        }

        private List<AuditTableConfig> GetAuditTableConfigurations(List<Type> targetTables)
        {
            var auditEntities = (targetTables ?? typeof(AuditEntryDTO).Assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<AuditTableAttribute>() != null))
                .Select(t =>
                {
                    var attr = t.GetCustomAttribute<AuditTableAttribute>()!;
                    var entityType = _context.Model.FindEntityType(t);

                    if (entityType == null)
                    {
                        throw new InvalidOperationException($"Entity {t.Name} not found in DbContext.");
                    }

                    var primaryKey = entityType.FindPrimaryKey();

                    if (primaryKey == null || primaryKey.Properties.Count == 0)
                    {
                        throw new InvalidOperationException($"Entity {entityType.Name} does not have a primary key defined.");
                    }

                    var primaryKeyProperty = primaryKey.Properties[0];

                    var columnsToAudit = entityType
                        .GetProperties()
                        .Where(p => !p.IsShadowProperty()
                                    && (IncludeId || p.Name != "ID" && p.Name != "Id")
                                    && p.Name != "SysStartTime"
                                    && p.Name != "SysEndTime"
                                    && p.Name != "LastModifiedBy"
                                    && p.Name != "Reason"
                                    && p.Name != "OwnerUserId"
                                    && (!SiteIdAsColumn || p.Name != "SiteId" && p.Name != "SiteID"))
                        .Select(p => (p.Name, PropertyType: p.ClrType))
                        .ToList();

                    var parentTableName = entityType.GetTableName();

                    var jsonColumnsInTable = entityType.GetNavigations()
                        .Where(n => n.TargetEntityType.IsOwned()
                                    && (n.TargetEntityType.GetTableName() == parentTableName
                                        || string.IsNullOrEmpty(n.TargetEntityType.GetTableName())))
                        .Select(n => (n.Name, PropertyType: typeof(string)))
                        .ToList();

                    columnsToAudit.AddRange(jsonColumnsInTable);

                    return new AuditTableConfig
                    {
                        TableType = t,
                        Schema = entityType.GetSchema() ?? entityType.Model.GetDefaultSchema() ?? "public",
                        TableName = entityType.GetTableName() ?? t.Name,
                        SiteJoin = attr.SiteJoin,
                        SiteIdColumn = attr.SiteIdColumn,
                        RelationFilterSql = attr.RelationFilterSql,
                        RecordIdColumn = primaryKeyProperty.Name,
                        RecordIdClrType = primaryKeyProperty.ClrType,
                        RecordIdColumnType = primaryKeyProperty.GetColumnType(),
                        ColumnsToAudit = columnsToAudit,
                        HasReason = entityType.GetProperties().Any(p => p.Name == "Reason"),
                        IsSystemVersioned = entityType.FindAnnotation("SqlServer:IsTemporal") != null,
                        TimestampExpression = attr.TimestampExpression,
                        UserIdColumnExpression = attr.UserIdColumnExpression,
                    };
                })
                .ToList();

            return auditEntities;
        }

        private string LoadTemplate(string fileName)
        {
            var assembly = typeof(AuditHelper).Assembly;
            var resourceName = $"{assembly.GetName().Name}.Templates.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource not found: {resourceName}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public async Task<MemoryStream> FormatAuditTrailFile(
            List<AuditEntryDTO> changes,
            Dictionary<int, string> usersLookup)
        {
            var ms = new MemoryStream();

            using (var streamWriter = new StreamWriter(ms, Encoding.GetEncoding("ISO-8859-1"), bufferSize: 1024, leaveOpen: true))
            {
                var header = SiteIdAsColumn
                    ? "Record Id,User Id,Timestamp,Action,Site Id,Table,Field,Old Value,New Value,Reason"
                    : "Record Id,User Id,Timestamp,Action,Table,Field,Old Value,New Value,Reason";

                await streamWriter.WriteLineAsync(header);

                foreach (var item in changes)
                {
                    if (!string.IsNullOrEmpty(item.NewValue) && (item.NewValue.StartsWith('{') || item.NewValue.StartsWith('[')))
                    {
                        item.NewValue = FormatJsonValue(item.NewValue);
                    }

                    if (!string.IsNullOrEmpty(item.OldValue) && (item.OldValue.StartsWith('{') || item.OldValue.StartsWith('[')))
                    {
                        item.OldValue = FormatJsonValue(item.OldValue);
                    }

                    await streamWriter.WriteLineAsync(
                        $"{item.RecordId}," +
                        $"{ResolveUserName(item.UserId, usersLookup)}," +
                        $"{item.Timestamp.ToString(DateTimePattern, System.Globalization.CultureInfo.InvariantCulture)}," +
                        $"{item.ActionType}," +
                        (SiteIdAsColumn ? $"{item.SiteId}," : "") +
                        $"{item.TableName}," +
                        $"{item.FieldName}," +
                        $"{item.OldValue}," +
                        $"{(item.ActionType == "Delete" ? null : item.NewValue)}," +
                        $"{item.Reason}"
                    );
                }

                await streamWriter.FlushAsync();
            }

            ms.Position = 0;
            return ms;
        }

        public List<ModuleAuditTrailDTO> FormatAuditTrailResult(
            List<AuditEntryDTO> changes,
            Dictionary<int, string> usersLookup)
        {
            var response = new List<ModuleAuditTrailDTO>();

            foreach (var item in changes)
            {
                var newItem = new ModuleAuditTrailDTO
                {
                    RecordId = item.RecordId,
                    UserIdValue = item.UserId,
                    UserName = ResolveUserName(item.UserId, usersLookup),
                    Timestamp = item.Timestamp,
                    ActionType = item.ActionType,
                    SiteId = item.SiteId,
                    Table = item.TableName,
                    Field = item.FieldName,
                    OldValue = item.OldValue,
                    NewValue = item.ActionType == "Delete" ? null : item.NewValue,
                    Reason = item.Reason
                };

                response.Add(newItem);
            }

            return response;
        }

        private static string ResolveUserName(int? userId, Dictionary<int, string> usersLookup)
        {
            if (!userId.HasValue)
            {
                return "N/A";
            }

            if (userId.Value == 0)
            {
                return "System";
            }

            return usersLookup.TryGetValue(userId.Value, out var userName)
                ? userName
                : userId.Value.ToString();
        }

        private static string FormatJsonValue(string colVal)
        {
            if (colVal == null)
            {
                return "{}";
            }

            var cleaned = colVal
                .Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\"", "\"\"");

            return $"\"{cleaned}\"";
        }
    }
}
