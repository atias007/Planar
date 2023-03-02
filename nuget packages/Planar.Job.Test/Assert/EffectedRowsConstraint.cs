using Planar.Job.Test.Common;
using System.Globalization;

namespace Planar.Job.Test
{
    public class EffectedRowsConstraint : BasePlanarConstraint
    {
        internal EffectedRowsConstraint(IJobExecutionResult result) : base(result)
        {
        }

        public AssertPlanarConstraint IsGreaterThen(int value)
        {
            if (_result.EffectedRows > value) { return Assert; }
            var message = $"Expect to be greater then {value} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsGreaterOrEqualsTo(int value)
        {
            if (_result.EffectedRows >= value) { return Assert; }
            var message = $"Expect to be greater or equals to {value} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsLessThen(int value)
        {
            if (_result.EffectedRows < value) { return Assert; }
            var message = $"Expect to be less then to {value} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsLessOrEqualsTo(int value)
        {
            if (_result.EffectedRows <= value) { return Assert; }
            var message = $"Expect to be less then or equals to {value} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsBetweenExclusive(int from, int to)
        {
            if (_result.EffectedRows > from && _result.EffectedRows < to) { return Assert; }
            var message = $"Expect to be between exclusive {from} to {to} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsBetweenInclusive(int from, int to)
        {
            if (_result.EffectedRows >= from && _result.EffectedRows <= to) { return Assert; }
            var message = $"Expect to be between inclusive {from} to {to} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNotBetweenExclusive(int from, int to)
        {
            if (_result.EffectedRows < from || _result.EffectedRows > to) { return Assert; }
            var message = $"Expect to be not between exclusive {from} to {to} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNotBetweenInclusive(int from, int to)
        {
            if (_result.EffectedRows <= from || _result.EffectedRows >= to) { return Assert; }
            var message = $"Expect to be between inclusive {from} to {to} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsEqualsTo(int value)
        {
            if (_result.EffectedRows == value) { return Assert; }
            var message = $"Expect to be equals to {value} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNotEqualsTo(int value)
        {
            if (_result.EffectedRows != value) { return Assert; }
            var message = $"Expect to be not equals to {value} but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsZero()
        {
            if (_result.EffectedRows == 0) { return Assert; }
            var message = $"Expect to be 0 but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNull()
        {
            if (_result.EffectedRows == null) { return Assert; }
            var message = $"Expect to be null but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsEmpty()
        {
            if (_result.EffectedRows.GetValueOrDefault() == 0) { return Assert; }
            var message = $"Expect to be empty (null or zero) but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNotNull()
        {
            if (_result.EffectedRows != null) { return Assert; }
            var message = $"Expect to be not null but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint IsNotEmpty()
        {
            if (_result.EffectedRows.GetValueOrDefault() > 0) { return Assert; }
            var message = $"Expect to be not empty (null or zero) but current value is {GetEffectedRowsString(_result.EffectedRows)}";
            throw new AssertPlanarException(message);
        }

        private static string GetEffectedRowsString(int? value)
        {
            if (value == null) { return null; }
            return value.ToString();
        }
    }
}