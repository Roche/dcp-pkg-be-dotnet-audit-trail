using System.ComponentModel;

namespace OpenDCP.Framework.AuditTrail.Constants
{
    public enum AuditTrailSecurityActions
    {
        [Description("Login")]
        Login = 1,
        [Description("Logout")]
        Logout = 2,
        [Description("Login Fail")]
        LoginFail = 3,
        [Description("Rest Token Successfully")]
        RestTokenSuccessfully = 4,
        [Description("Rest Token Request Fail")]
        RestTokenRequestFail = 5
    }
}
