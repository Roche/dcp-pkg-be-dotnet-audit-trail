using System;
using System.Text.Json.Nodes;

namespace OpenDCP.Framework.AuditTrail.Models
{
    public class AdminActionAuditTrailDTO
    {
        public int? UserId { get; set; }
        /// <summary>
        /// AdminAction = 1
        /// </summary>
        public int? Action { get; set; }
        public DateTime? Timestamp { get; set; }
        public JsonObject ActionConfiguration { get; set; }
        public string Reason { get; set; }
        public string Module { get; set; }
        public string Exception { get; set; }
        public bool IsGxp { get; set; }
    }
}
