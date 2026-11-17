namespace OpenDCP.Framework.AuditTrail.Interfaces
{
    public interface IAuditTrail
    {
        int? LastModifiedBy { get; set; }
        int? OwnerUserId { get; set; }
    }
}
