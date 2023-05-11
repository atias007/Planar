using Microsoft.Extensions.Logging;
using Planar.Job;

namespace SomeJob
{
    public class GeneralUtil
    {
        public GeneralUtil(ILogger<GeneralUtil> logger)
        {
            logger.LogInformation("Success :))))");
        }
    }
}