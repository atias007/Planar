﻿using HelloWorld;
using Planar.Job;

PlanarJob.Debugger.AddProfile("Dev1", b => b.WithExecutionDate(DateTime.Now.AddMonths(-5)));

PlanarJob.Start<Job>();