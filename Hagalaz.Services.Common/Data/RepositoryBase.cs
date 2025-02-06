using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hagalaz.Services.Common.Data
{
    public abstract class RepositoryBase<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _dbSet;

        protected RepositoryBase(DbContext context) => _dbSet = context.Set<TEntity>();

        public EntityEntry<TEntity> Add(TEntity entity) => _dbSet.Add(entity);
        
        public EntityEntry<TEntity> Remove(TEntity entity) => _dbSet.Remove(entity);

        public EntityEntry<TEntity> Update(TEntity entity) => _dbSet.Update(entity);

        public IQueryable<TEntity> FindAll() => _dbSet;
    }
}