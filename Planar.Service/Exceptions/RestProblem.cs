using System;
using System.Collections.Generic;

namespace Planar.Service.Exceptions
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

    public class RestProblem : IEquatable<RestProblem>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
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
    }
}