using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class HechoPostgreSqlAttribute : FactAttribute
{
    public HechoPostgreSqlAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTest")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")))
        {
            Skip = "Configure ConnectionStrings__FlexPosTest y ConnectionStrings__FlexPosTestMigraciones para PostgreSQL.";
        }
    }
}
