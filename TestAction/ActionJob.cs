using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
using System;
using System.Threading.Tasks;

namespace TestAction
{
    public class ActionJob : BaseJob
    {
        public string Message { get; set; }

        public double Value { get; set; }

        public int MaxId { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            if (Value == 100.1)
            {
                for (int i = 0; i < 130; i++)
                {
                    UpdateProgress(i, 130);
                    SetEffectedRows(i + 1);
                    if (CheckIfStopRequest())
                    {
                        Logger.LogInformation("Cancel job");
                        break;
                    }
                    if (i % 10 == 0)
                    {
                        Logger.LogInformation($"Step {i}");
                    }
                    await Task.Delay(1000);
                }
            }
            else if (Value == 100.2)
            {
                PutJobData(nameof(MaxId), ++MaxId);
                throw new ArgumentException("This is exception test");
            }
            else
            {
                SetEffectedRows(DateTime.Now.Second);
            }

            var greetings = Configuration.GetValue<string>("JobSet1");
            Logger.LogInformation($"[x] Greetings from ActionJob ({greetings})! [{Now():dd/MM/yyyy HH:mm}] {Message}, {Value:N1}, MaxId: {MaxId}");

            PutJobData(nameof(MaxId), ++MaxId);
        }

        public override void RegisterServices(IServiceCollection services)
        {
        }
    }
}