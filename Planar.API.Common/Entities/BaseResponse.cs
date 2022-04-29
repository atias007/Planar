namespace Planar.API.Common.Entities
{
    public class BaseResponse
    {
        public bool Success { get; set; } = true;

        public string ErrorDescription { get; set; }

        public int ErrorCode { get; set; }

        public static BaseResponse Empty
        {
            get
            {
                return new BaseResponse();
            }
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public BaseResponse(T result)
        {
            Result = result;
        }

        public BaseResponse()
        {
            Result = default;
        }

        public T Result { get; set; }
    }
}