﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System;
using System.Threading.Tasks;

namespace TestAction
{
    public class Job : BaseJob
    {
        public string Message { get; set; }

        public double Value { get; set; }

        public int MaxId { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            Logger.LogInformation("Start Exist...");
            await Task.Delay(3_000);
            await base.RaiseCustomEventAsync(CustomMonitorEvents.CustomEvent1, "This is demo custom event");
            return;
            if (Value == 100.1)
            {
                for (int i = 0; i < 130; i++)
                {
                    await UpdateProgressAsync(i, 130);
                    await SetEffectedRowsAsync(i + 1);

                    if (i % 10 == 0)
                    {
                        Logger.LogInformation("Step {Index}", i);
                    }
                    await Task.Delay(1000);
                }
            }
            else if (Value == 100.2)
            {
                await PutJobDataAsync(nameof(MaxId), ++MaxId);
                throw new ArgumentException("This is exception test");
            }
            else
            {
                await SetEffectedRowsAsync(DateTime.Now.Second);
            }

            var greetings = Configuration.GetValue<string>("JobSet1");
            Logger.LogInformation("[x] Greetings from ActionJob ({Greetings})! [{Now:dd/MM/yyyy HH:mm}] {Message}, {Value:N1}, MaxId: {MaxId}", greetings, Now(), Message, Value, MaxId);

            await PutJobDataAsync(nameof(MaxId), ++MaxId);
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}