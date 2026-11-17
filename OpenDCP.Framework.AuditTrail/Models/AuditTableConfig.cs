using System;
using System.Collections.Generic;

namespace OpenDCP.Framework.AuditTrail.Models
{
    public class AuditTableConfig
    {
        public Type TableType { get; set; }
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string SiteJoin { get; set; }
        public string SiteIdColumn { get; set; }
        public string RelationFilterSql { get; set; }
        public string RecordIdColumn { get; set; }
        public Type RecordIdClrType { get; set; }
        public string RecordIdColumnType { get; set; }
        public List<(string Name, Type PropertyType)> ColumnsToAudit { get; set; } = new();
        public bool HasReason { get; set; }
        public bool IsSystemVersioned { get; set; }
        public string TimestampExpression { get; set; }
        public string UserIdColumnExpression { get; set; }
    }
}
