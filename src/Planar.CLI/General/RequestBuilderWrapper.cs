using RestSharp;

namespace Planar.CLI.General
{
    internal class RequestBuilderWrapper<T>
    {
        public T Request { get; set; }
        public RestResponse FailResponse { get; set; }
        public bool IsSuccessful => FailResponse == null;
    }
}