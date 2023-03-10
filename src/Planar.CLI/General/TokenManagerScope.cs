using System;
using System.Threading;

namespace Planar.CLI.General
{
    public sealed class TokenManagerScope : IDisposable
    {
        public TokenManagerScope()
        {
            TokenManager.Reset();
        }

        public static CancellationToken Token
        {
            get { return TokenManager.Token; }
        }

        public void Dispose()
        {
            TokenManager.Reset();
            TokenManager.Release();
        }
    }
}