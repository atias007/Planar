using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Planar.Service.General
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
    {
        public CancellationTokenAwaiter(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        internal CancellationToken CancellationToken;

        public object GetResult()
        {
            // this is called by compiler generated methods when the
            // task has completed. Instead of returning a result, we
            // just throw an exception.
            if (IsCompleted) throw new OperationCanceledException();
            else throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
        }

        // called by compiler generated/.net internals to check
        // if the task has completed.
        public bool IsCompleted => CancellationToken.IsCancellationRequested;

        // The compiler will generate stuff that hooks in
        // here. We hook those methods directly into the
        // cancellation token.
        public void OnCompleted(Action continuation) =>
            CancellationToken.Register(continuation);

        public void UnsafeOnCompleted(Action continuation) =>
            CancellationToken.Register(continuation);
    }
}