using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Planar.Client
{
    public interface IRestResponse
    {
        bool IsSuccess { get; }
        HttpResponseMessage Response { get; }
        HttpStatusCode StatusCode { get; }

        Task<TResponse> GetData<TResponse>() where TResponse : class;

        Task<string> GetStringContent();
    }
}