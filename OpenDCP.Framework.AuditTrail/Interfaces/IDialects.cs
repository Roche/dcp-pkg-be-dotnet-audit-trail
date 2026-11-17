using OpenDCP.Framework.AuditTrail.Models;
using System;
using System.Collections.Generic;

namespace OpenDCP.Framework.AuditTrail.Interfaces
{
    public interface IDialects
    {
        public string TemplatePrefix { get; }

        public string LoadTemplate(string templateName, Func<string, string> loadTemplate);

        public abstract string BuildColumns(IEnumerable<(string Name, Type PropertyType)> columnsToAudit, string dateTimePattern);

        public abstract string BuildUnpivotFields(IEnumerable<(string Name, Type PropertyType)> columnsToAudit);

        public string BuildSiteIdExpression(AuditTableConfig cfg);

        public string BuildReasonColumn(bool hasReason);

        public string BuildWhereClause(AuditTableConfig cfg, object recordId, int? siteId,
            DateTime? startTime, DateTime? endTime);

        public string BuildInnerTemporalClause(DateTime? start, DateTime? end);

        public string BuildOuterTemporalFilter(DateTime? start, DateTime? end);
        public string BuildMetaDataRecordIdPredicate(AuditTableConfig cfg);
    }
}
