using IdGen;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Vivcord.Server.DbContext;

public class SnowflakeValueGenerator : ValueGenerator<long>
{
    private static readonly IdGenerator _generator = new(0);
    public override bool GeneratesTemporaryValues => false;
    public override long Next(EntityEntry entry) => _generator.CreateId();
}
