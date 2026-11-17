using System;

namespace OpenDCP.Framework.AuditTrail.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AuditTableAttribute : Attribute
    {
        public string SiteIdColumn { get; set; } = "SiteId";
        public string SiteJoin { get; set; } = null;
        public string RelationFilterSql { get; set; }
        public string TimestampExpression { get; set; } = null;
        public string UserIdColumnExpression { get; set; } = null;

        public AuditTableAttribute(
            string siteIdColumn = "SiteId",
            string siteJoin = null,
            string relationFilterSql = "",
            string timestampExpression = null,
            string userIdColumnExpression = null)
        {
            SiteIdColumn = siteIdColumn;
            SiteJoin = siteJoin;
            RelationFilterSql = relationFilterSql;
            TimestampExpression = timestampExpression;
            UserIdColumnExpression = userIdColumnExpression;
        }
    }
}
