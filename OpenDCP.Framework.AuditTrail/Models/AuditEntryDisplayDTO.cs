using System;

namespace OpenDCP.Framework.AuditTrail.Models
{
    public class AuditEntryDTO
    {
        private string _reason;

        public string RecordId { get; set; }
        public int? SiteId { get; set; }
        public string TableName { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; }
        public int? UserId { get; set; }
        public string Reason
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_reason))
                {
                    return _reason;
                }

                string actor = UserId == 0 ? "System" : "User";
                return $"System generated - {actor} {ActionType}";
            }
            set => _reason = value;
        }
    }
}
