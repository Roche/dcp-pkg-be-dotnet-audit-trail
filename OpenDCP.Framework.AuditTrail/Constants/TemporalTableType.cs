namespace OpenDCP.Framework.AuditTrail.Constants
{
    public enum TemporalTableType
    {
        NonTemporalTable = 0,
        /// <summary>
        /// Associated with a temporal table
        /// </summary>
        HistoryTable = 1,
        SystemVersionedTemporalTable = 2,
    }
}
