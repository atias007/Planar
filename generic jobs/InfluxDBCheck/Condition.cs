using System.Text.RegularExpressions;

namespace InfluxDBCheck
{
    internal enum Operator
    {
        Eq,
        Ne,
        Gt,
        Ge,
        Lt,
        Le,
        Be,
        Bi
    }

    internal sealed class Condition
    {
        public Operator Operator { get; set; }
        public double Value1 { get; set; }
        public double? Value2 { get; set; }

        public Condition(Match match)
        {
            var group = match.Groups[0];
            var op = group.Captures[0].Value;
            if (!Enum.TryParse<Operator>(op, ignoreCase: true, out var @operator))
            {
                throw new ArgumentException($"Invalid operator: {op}");
            }

            if (@operator == Operator.Be || @operator == Operator.Bi)
            {
                var value1Text = group.Captures[1].Value;
                if (!double.TryParse(value1Text, out var tmpValue1))
                {
                    throw new ArgumentException($"Invalid value: {value1Text}");
                }

                var value2Text = group.Captures[3].Value;
                if (!double.TryParse(value2Text, out var tmpValue2))
                {
                    throw new ArgumentException($"Invalid value: {value2Text}");
                }

                Value1 = tmpValue1;
                Value2 = tmpValue2;
            }
            else
            {
                var valueText = group.Captures[1].Value;
                if (!double.TryParse(valueText, out var tmpValue1))
                {
                    throw new ArgumentException($"Invalid value: {valueText}");
                }

                Value1 = tmpValue1;
            }
        }

        public bool Evaluate(double value)
        {
            return Operator switch
            {
                Operator.Eq => FloatEquals(value, Value1),
                Operator.Ne => !FloatEquals(value, Value1),
                Operator.Gt => value > Value1,
                Operator.Ge => value >= Value1,
                Operator.Lt => value < Value1,
                Operator.Le => value <= Value1,
                Operator.Be => value >= Value1 && value <= Value2,
                Operator.Bi => value > Value1 && value < Value2,
                _ => throw new InvalidOperationException($"Unknown operator: {Operator}"),
            };
        }

        private static bool FloatEquals(double a, double b)
        {
            const double epsilon = 0.0001;
            return Math.Abs(a - b) < epsilon;
        }
    }
}