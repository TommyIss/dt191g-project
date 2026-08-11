using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace dt191g_project.Data
{
    public class SqliteModelCacheKeyFactory: IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
            => new { Type = context.GetType(), DesignTime = designTime, UseSqlite = true };
    }
}
