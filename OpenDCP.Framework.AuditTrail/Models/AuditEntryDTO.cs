using System;

namespace OpenDCP.Framework.AuditTrail.Models
{
    public class AuditEntryDisplayDTO
    {
        public string RecordId { get; set; }
        public string TableName { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string Reason { get; set; }
    }
}
