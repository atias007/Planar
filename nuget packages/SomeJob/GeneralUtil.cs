using Microsoft.Extensions.Logging;

namespace SomeJob
{
    public class GeneralUtil
    {
        public GeneralUtil(ILogger<GeneralUtil> logger)
        {
            logger.LogInformation("Success :)");
        }
    }
}