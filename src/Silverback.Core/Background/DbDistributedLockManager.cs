﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Background.Model;
using Silverback.Database;

namespace Silverback.Background
{
    public class DbDistributedLockManager : IDistributedLockManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public DbDistributedLockManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<DbDistributedLockManager>>();
        }

        public Task<DistributedLock> Acquire(DistributedLockSettings settings, CancellationToken cancellationToken = default) =>
            Acquire(settings.ResourceName, settings.AcquireTimeout, settings.AcquireRetryInterval, settings.HeartbeatTimeout, cancellationToken);

        public async Task<DistributedLock> Acquire(string resourceName, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null, CancellationToken cancellationToken = default)
        {
            var start = DateTime.Now;
            while (acquireTimeout == null || DateTime.Now - start < acquireTimeout)
            {
                if (await TryAcquireLock(resourceName, heartbeatTimeout))
                    return new DistributedLock(resourceName, this);

                await Task.Delay(acquireRetryInterval?.Milliseconds ?? 500, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            throw new TimeoutException($"Timeout waiting to get the required lock '{resourceName}'.");
        }

        public async Task SendHeartbeat(string resourceName)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                await SendHeartbeat(resourceName, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send heartbeat for lock '{lockName}'. See inner exception for details.",
                    resourceName);
            }
        }

        public async Task Release(string resourceName)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                await Release(resourceName, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock '{lockName}'. See inner exception for details.", resourceName);
            }
        }

        private async Task<bool> TryAcquireLock(string resourceName, TimeSpan? heartbeatTimeout = null)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                return await AcquireLock(resourceName, heartbeatTimeout, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire lock '{lockName}'. See inner exception for details.",
                    resourceName);
            }

            return false;
        }

        private async Task<bool> AcquireLock(string resourceName, TimeSpan? heartbeatTimeout, IServiceProvider serviceProvider)
        {
            var heartbeatThreshold = DateTime.UtcNow.Subtract(heartbeatTimeout ?? TimeSpan.FromSeconds(10));

            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            if (await dbSet.AsQueryable().AnyAsync(l => l.Name == resourceName && l.Heartbeat >= heartbeatThreshold))
                return false;

            await WriteLock(resourceName, dbSet, dbContext);

            return true;
        }

        private async Task WriteLock(string resourceName, IDbSet<Lock> dbSet, IDbContext dbContext)
        {
            var entity = await dbSet.AsQueryable().FirstOrDefaultAsync(e => e.Name == resourceName)
                         ?? dbSet.Add(new Lock { Name = resourceName });

            entity.Heartbeat = entity.Created = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }
        
        private async Task SendHeartbeat(string resourceName, IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = await dbSet.AsQueryable().FirstOrDefaultAsync(l => l.Name == resourceName);

            if (lockRecord == null)
                return;

            lockRecord.Heartbeat = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }
        
        private async Task Release(string resourceName, IServiceProvider serviceProvider)
        {
            var (dbSet, dbContext) = GetDbSet(serviceProvider);

            var lockRecord = await dbSet.AsQueryable().FirstOrDefaultAsync(l => l.Name == resourceName);

            if (lockRecord == null)
                return;

            dbSet.Remove(lockRecord);

            await dbContext.SaveChangesAsync();
        }

        private (IDbSet<Lock> dbSet, IDbContext dbContext) GetDbSet(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<IDbContext>();
            var dbSet = dbContext.GetDbSet<Lock>();

            return (dbSet, dbContext);
        }
    }
}