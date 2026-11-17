namespace OpenDCP.Framework.AuditTrail.Interfaces
{
    public interface IHistoryAuditTrail
    {
        /// <summary>
        /// Included only for the Audit trail. This column is not presented to DB model
        /// </summary>
        int ActionType { get; set; }
        /// <summary>
        /// Included only for the Audit trail. This column is not presented to DB model
        /// </summary>
        int SiteId { get; set; }
    }
}
