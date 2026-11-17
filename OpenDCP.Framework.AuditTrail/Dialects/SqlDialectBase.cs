using OpenDCP.Framework.AuditTrail.Interfaces;
using OpenDCP.Framework.AuditTrail.Models;
using System;
using System.Collections.Generic;

namespace OpenDCP.Framework.AuditTrail.Dialects
{
    public abstract class SqlDialectBase : IDialects
    {
        public abstract string TemplatePrefix { get; }

        public string LoadTemplate(string templateName, Func<string, string> loadTemplate)
        {
            return loadTemplate($"{TemplatePrefix}{templateName}");
        }

        public abstract string BuildColumns(IEnumerable<(string Name, Type PropertyType)> columnsToAudit, string dateTimePattern);

        public abstract string BuildUnpivotFields(IEnumerable<(string Name, Type PropertyType)> columnsToAudit);

        public abstract string BuildSiteIdExpression(AuditTableConfig cfg);

        public abstract string BuildReasonColumn(bool hasReason);

        public abstract string BuildWhereClause(AuditTableConfig cfg, object recordId, int? siteId,
            DateTime? start, DateTime? end);

        public abstract string BuildInnerTemporalClause(DateTime? start, DateTime? end);

        public abstract string BuildOuterTemporalFilter(DateTime? start, DateTime? end);
        public abstract string BuildMetaDataRecordIdPredicate(AuditTableConfig cfg);
    }
}
