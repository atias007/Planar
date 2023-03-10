using System;

namespace Planar.CLI.General
{
    public sealed class TokenBlockerScope : IDisposable
    {
        public TokenBlockerScope()
        {
            TokenManager.Block();
        }

        public void Dispose()
        {
            TokenManager.Release();
        }
    }
}