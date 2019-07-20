using System;
using System.Threading;

namespace LogGrokCore.Data.Index
{
    public static class ReaderWriterLockSlimExtensions
    {
        public readonly struct ReadLockOwner : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public ReadLockOwner(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterReadLock();
            }

            public void Dispose()
            {
                _lock.ExitReadLock();
            }
        }

        public readonly struct UpgradableReadLockOwner : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public UpgradableReadLockOwner (ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterUpgradeableReadLock();
            }

            public void Dispose()
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        
        public readonly struct WriteLockOwner : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public WriteLockOwner(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterWriteLock();
            }

            public void Dispose()
            {
                _lock.ExitWriteLock();
            }
        }

        public static ReadLockOwner GetReadLockOwner(this ReaderWriterLockSlim @lock)
        {
            return new ReadLockOwner(@lock);
        }
        
        public static UpgradableReadLockOwner GetUpgradableReadLockOwner(this ReaderWriterLockSlim @lock)
        {
            return new UpgradableReadLockOwner (@lock);
        }
        
        public static WriteLockOwner GetWriteLockOwner(this ReaderWriterLockSlim @lock)
        {
            return new WriteLockOwner(@lock);
        }

    }
}