﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar;
using Planar.Job;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestAggEx
{
    public class MyJob : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder)
        {
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                try
                {
                    if (i % 3 == 0)
                    {
                        throw new PlanarJobException($"This is my ex with id {i}");
                    }
                    IncreaseEffectedRows();
                }
                catch (Exception ex)
                {
                    AddAggragateException(ex);
                }
                finally
                {
                    UpdateProgress(i + 1, 10);
                }
            }

            CheckAggragateException();

            return Task.CompletedTask;
        }

        public override void RegisterServices(IServiceCollection services)
        {
        }
    }
}