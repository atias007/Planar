using System.Globalization;
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

    internal sealed partial class Condition
    {
        public Operator Operator { get; set; }
        public double Value1 { get; set; }
        public double? Value2 { get; set; }
        public string Text { get; set; }

        public Condition(Match match)
        {
            Text = match.Groups[0].Value;
            var op = match.Groups[1].Value;
            if (!Enum.TryParse<Operator>(op, ignoreCase: true, out var @operator))
            {
                throw new ArgumentException($"Invalid operator: {op}");
            }

            Operator = @operator;
            if (@operator == Operator.Be || @operator == Operator.Bi)
            {
                var values = ExtractNumbers(Text);
                if (values.Count != 2)
                {
                    throw new ArgumentException($"Invalid value: {Text}");
                }

                Value1 = values[0];
                Value2 = values[1];
            }
            else
            {
                var values = ExtractNumbers(Text);
                if (values.Count != 1)
                {
                    throw new ArgumentException($"Invalid value: {Text}");
                }

                Value1 = values[0];
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

        private static List<double> ExtractNumbers(string input)
        {
            var regex = NumerigRegex();
            var matches = regex.Matches(input);
            return matches.Cast<Match>().Select(m => double.Parse(m.Value, CultureInfo.CurrentCulture)).ToList();
        }

        [GeneratedRegex(@"[-+]?\d+(\.\d+)?")]
        private static partial Regex NumerigRegex();
    }
}