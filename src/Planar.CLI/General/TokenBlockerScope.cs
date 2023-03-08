using System;

namespace Planar.CLI.General
{
    public class TokenBlockerScope : IDisposable
    {
        public TokenBlockerScope()
        {
            TokenManager.Block();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            TokenManager.Release();
        }
    }
}