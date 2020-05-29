using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DockerizedTesting
{
    public sealed class CompositeDisposable : IReadOnlyCollection<IDisposable>, IDisposable
    {
        private List<IDisposable> disposables = new List<IDisposable>();
        public CompositeDisposable()
        {
        }

        public void Add(IDisposable d)
        {
            lock (disposeLock)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(CompositeDisposable));
                }
                disposables.Add(d);
            }
        }

        public int Count => ((IReadOnlyCollection<IDisposable>)disposables).Count;

        public IEnumerator<IDisposable> GetEnumerator()
        {
            return ((IReadOnlyCollection<IDisposable>)disposables).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyCollection<IDisposable>)disposables).GetEnumerator();
        }

        private bool disposed = false;
        private object disposeLock = new object();
        public void Dispose()
        {
            IList<IDisposable> toDispose = null;

            lock (disposeLock)
            {
                if (!disposed)
                {
                    toDispose = disposables;
                    Volatile.Write(ref disposed, true);
                    disposables = null;
                }
            }

            if (toDispose == null)
            {
                return;
            }
            foreach (var d in toDispose)
            {
                d.Dispose();
            }
        }
    }
}