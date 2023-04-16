using System;
using System.Collections.Generic;

namespace Planar.Service.Exceptions
{
    public sealed class RestProblem : IEquatable<RestProblem>
    {
        public RestProblem()
        {
        }

        public RestProblem(string field)
        {
            Field = field;
        }

        public RestProblem(string field, string detail)
            : this(field)
        {
            Detail.Add(detail);
        }

        public RestProblem(string field, string detail, int errorCode)
            : this(field, detail)
        {
            ErrorCode = errorCode;
        }

        public string? Field { get; set; }

        public List<string> Detail { get; set; } = new();

        public int ErrorCode { get; set; }

        public bool Equals(RestProblem? other)
        {
            if (Field == null) { return other == null; }
            return Field.Equals(other?.Field);
        }

        public override bool Equals(object? obj)
        {
            if (obj is RestProblem prob)
            {
                return Equals(prob);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return $"{Field}|{Detail}".GetHashCode();
        }
    }
}