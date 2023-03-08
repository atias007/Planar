using System.Threading;

namespace Planar.CLI.General
{
    public static class TokenManager
    {
        private static readonly object Lock = new();
        private static CancellationTokenSource? _tokenSource;
        public static bool _blocked;

        public static CancellationToken Token
        {
            get
            {
                lock (Lock)
                {
                    if (_tokenSource == null) { return default; }
                    return _tokenSource.Token;
                }
            }
        }

        public static void Reset()
        {
            lock (Lock)
            {
                if (_tokenSource == null)
                {
                    _tokenSource = new CancellationTokenSource();
                    return;
                }

                if (_tokenSource.TryReset() == false)
                {
                    _tokenSource = new CancellationTokenSource();
                }
            }
        }

        public static void Block()
        {
            lock (Lock)
            {
                _blocked = true;
            }
        }

        public static void Release()
        {
            lock (Lock)
            {
                _blocked = false;
            }
        }

        public static void Cancel()
        {
            lock (Lock)
            {
                if (_blocked) { return; }
                _tokenSource?.Cancel();
            }
        }
    }
}