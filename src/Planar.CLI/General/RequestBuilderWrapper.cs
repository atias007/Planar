using RestSharp;

namespace Planar.CLI.General
{
    internal class RequestBuilderWrapper<T>
    {
        public T? Request { get; set; }
        public RestResponse? FailResponse { get; init; }
        public bool IsSuccessful => FailResponse == null;

        public static RequestBuilderWrapper<T> Fail(RestResponse? failResponse) => new() { FailResponse = failResponse };
    }
}