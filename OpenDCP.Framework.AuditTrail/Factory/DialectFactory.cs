using Microsoft.EntityFrameworkCore;
using OpenDCP.Framework.AuditTrail.Dialects;
using OpenDCP.Framework.AuditTrail.Interfaces;
using System;

namespace OpenDCP.Framework.AuditTrail.Factory
{
    public static class DialectFactory
    {
        public static IDialects Create(DbContext context)
        {
            var providerName = context.Database.ProviderName;

            return providerName switch
            {
                "Npgsql.EntityFrameworkCore.PostgreSQL" => new PostgreSqlDialect(),
                "Microsoft.EntityFrameworkCore.SqlServer" => new MsSqlServerDialect(),
                _ => throw new NotImplementedException(
                    $"Audit trail is not implemented for EF provider '{providerName}'.")
            };
        }
    }
}
