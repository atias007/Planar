﻿using Common;

namespace RabbitMQCheck;

internal class Defaults : BaseDefault
{
    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(10);
        MaximumFailsInRow = 5;
    }
}