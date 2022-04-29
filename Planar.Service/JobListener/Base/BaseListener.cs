using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar.Service.JobListener.Base
{
    public class BaseListener<T>
    {
        private readonly Singleton<ILogger<T>> _logger = new(GetLogger);

        private static ILogger<T> GetLogger()
        {
            return Global.ServiceProvider.GetService(typeof(ILogger<T>)) as ILogger<T>;
        }

        public ILogger<T> Logger
        {
            get
            {
                return _logger.Instance;
            }
        }
    }
}