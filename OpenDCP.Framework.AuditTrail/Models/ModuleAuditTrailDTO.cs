using System;

namespace OpenDCP.Framework.AuditTrail.Models
{
    public class ModuleAuditTrailDTO
    {
        public string RecordId { get; set; }
        public int? UserIdValue { get; set; }
        public int? SiteId { get; set; }
        public string Table { get; set; }
        public string Field { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; }
        public string UserName { get; set; }
        public string Reason { get; set; }
    }
}
