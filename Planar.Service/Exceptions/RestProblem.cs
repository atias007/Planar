using System;
using System.Collections.Generic;

namespace Planar.Service.Exceptions
{
    public class RestProblem : IEquatable<RestProblem>
    {
        public RestProblem()
        {
        }

        public RestProblem(string field)
        {
            Field = field;
        }

        public RestProblem(string field, string detail)
        {
            Field = field;
            Detail.Add(detail);
        }

        public string Field { get; set; }

        public List<string> Detail { get; set; } = new();

        public bool Equals(RestProblem other)
        {
            return Field.Equals(other.Field);
        }

        public override bool Equals(object obj)
        {
            if (obj is RestProblem prob)
            {
                return Equals(prob);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}