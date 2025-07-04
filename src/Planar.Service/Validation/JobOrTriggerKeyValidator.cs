﻿using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class JobOrTriggerKeyValidator : AbstractValidator<JobOrTriggerKey>
    {
        public JobOrTriggerKeyValidator()
        {
            RuleFor(r => r.Id).NotEmpty().Length(11, 101);
        }
    }
}