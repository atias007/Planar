using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    public class RestResponse<T> : RestResponse
        where T : class
    {
#if NETSTANDARD2_0

        internal RestResponse(RestResponse response, T data) : base(response.Response)
        {
            Data = data;
        }

        public T Data { get; private set; }

#else
        internal RestResponse(RestResponse response, T? data) : base(response.Response)
        {
            Data = data;
        }

        public T? Data { get; private set; }

#endif
    }

    public class RestResponse
    {
        private readonly HttpResponseMessage _response;
        private readonly SemaphoreSlim _lock1 = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _lock2 = new SemaphoreSlim(1, 1);
        private bool _isContentRead = false;
        private bool _isDataRead = false;

#if NETSTANDARD2_0

        private string _content;
#else
        private string? _content;
#endif

#if NETSTANDARD2_0

        private object _data;
#else
        private object? _data;
#endif

        public RestResponse(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpResponseMessage Response => _response;
        public HttpStatusCode StatusCode => _response.StatusCode;
        public bool IsSuccess => _response.IsSuccessStatusCode;

#if NETSTANDARD2_0

        public async Task<TResponse> GetData<TResponse>() where TResponse : class
#else
        public async Task<TResponse?> GetData<TResponse>() where TResponse : class
#endif

        {
            if (!_response.IsSuccessStatusCode) { return null; }
            if (_isDataRead) { return _data as TResponse; }
            await _lock1.WaitAsync();
            try
            {
                if (_isDataRead) { return _data as TResponse; }
                _isDataRead = true;
                var content = await GetStringContent();
                _data = CoreSerializer.Deserialize<TResponse>(content);
                return _data as TResponse;
            }
            finally
            {
                _lock1.Release();
            }
        }

#if NETSTANDARD2_0

        public async Task<string> GetStringContent()
#else
        public async Task<string?> GetStringContent()
#endif

        {
            if (_isContentRead) { return _content; }
            await _lock2.WaitAsync();
            try
            {
                if (_isContentRead) { return _content; }
                _isContentRead = true;
                _content = await _response.Content.ReadAsStringAsync();
                return _content;
            }
            finally
            {
                _lock2.Release();
            }
        }

        public async Task<RestResponse<T>> GetTypedResponse<T>() where T : class
        {
            var entity = await GetData<T>();
            return new RestResponse<T>(this, entity);
        }
    }
}