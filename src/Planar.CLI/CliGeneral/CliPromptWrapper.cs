using RestSharp;

namespace Planar.CLI.CliGeneral
{
    internal class CliPromptWrapper
    {
        protected CliPromptWrapper()
        {
        }

        public RestResponse? FailResponse { get; protected set; }
        public bool IsSuccessful => FailResponse == null;

        public static CliPromptWrapper Success => new();
    }

    internal class CliPromptWrapper<T> : CliPromptWrapper
    {
        public CliPromptWrapper(T? value)
        {
            Value = value;
        }

        public CliPromptWrapper(RestResponse? failResponse)
        {
            FailResponse = failResponse;
        }

        public T? Value { get; private set; }
    }
}