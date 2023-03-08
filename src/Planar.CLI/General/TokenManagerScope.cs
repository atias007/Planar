using System;
using System.Threading;

namespace Planar.CLI.General
{
    public class TokenManagerScope : IDisposable
    {
        public TokenManagerScope()
        {
            TokenManager.Reset();
        }

        public CancellationToken Token
        {
            get { return TokenManager.Token; }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            TokenManager.Reset();
            TokenManager.Release();
        }
    }
}