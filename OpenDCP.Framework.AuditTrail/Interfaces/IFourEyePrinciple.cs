namespace OpenDCP.Framework.AuditTrail.Interfaces
{
    public interface IFourEyePrinciple
    {
        int? ReviewAssignedToUserID { get; set; }
        byte Status { get; set; }
    }
}
