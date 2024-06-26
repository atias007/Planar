﻿using RestSharp;
using System;

namespace Planar.CLI
{
    public sealed class CliException : Exception
    {
        public RestResponse? RestResponse { get; private set; }

        public CliException(string message, RestResponse restResponse) : base(message)
        {
            RestResponse = restResponse;
        }

        public CliException(string message) : base(message)
        {
        }

        public CliException(string message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}