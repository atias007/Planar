using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Planar.Client
{
    public interface IRestResponse : IDisposable
    {
        bool IsSuccess { get; }
        HttpResponseMessage Response { get; }
        HttpStatusCode StatusCode { get; }

#if NETSTANDARD2_0

        Task<TResponse> GetData<TResponse>() where TResponse : class;

        Task<string> GetStringContent();

#else
        Task<TResponse?> GetData<TResponse>() where TResponse : class;
        Task<string?> GetStringContent();
#endif
    }
}