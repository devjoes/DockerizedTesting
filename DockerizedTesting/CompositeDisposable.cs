using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DockerizedTesting
{
    public sealed class CompositeDisposable : IReadOnlyCollection<IDisposable>, IDisposable, IAsyncDisposable
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

        private IList<IDisposable> getItemsToDispose()
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
            return toDispose;
        }

        public void Dispose()
        {
            var toDispose = getItemsToDispose();
            if (toDispose == null)
            {
                return;
            }
            foreach(var d in toDispose)
            {
                d.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            var toDispose = getItemsToDispose();
            if (toDispose == null)
            {
                return;
            }
            await Task.WhenAll(toDispose.Select(d => Task.Run(() => d.Dispose())));
        }
    }
}