﻿namespace WebUI.Domain.ObjectStore;

public interface IObjectCollection<TEntity> : IReadOnlyObjectCollection<TEntity>
    where TEntity : class
{
    Task<int> InsertAsync(TEntity model, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(ISpecification[] specification, TEntity model, CancellationToken cancellationToken = default);
}
