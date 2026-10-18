using System.Globalization;

namespace Tabsan.EduSphere.Application.Quizzes.Grading;

/// <summary>
/// Plan M2.3: small, safe arithmetic evaluator for Formula questions (recursive descent — no code
/// execution). Supports + - * / % ^, parentheses, unary signs, variables, the constants pi and e,
/// and the functions sqrt, abs, round(x[,digits]), floor, ceil, min, max, pow, exp, ln/log, log10,
/// sin, cos, tan (radians).
/// </summary>
public static class FormulaEvaluator
{
    private const int MaxLength = 500;

    public static double Evaluate(string expression, IReadOnlyDictionary<string, double> variables)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > MaxLength)
            throw new FormatException("Formula is empty or too long.");

        var parser = new Parser(expression, variables);
        var value = parser.ParseExpression();
        parser.ExpectEnd();
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new FormatException("Formula result is not a finite number.");
        return value;
    }

    /// <summary>Returns the variable names used by an expression (lower-case), excluding constants and functions.</summary>
    public static IReadOnlySet<string> GetVariableNames(string expression)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        while (i < expression.Length)
        {
            if (char.IsLetter(expression[i]) || expression[i] == '_')
            {
                var start = i;
                while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_')) i++;
                var name = expression[start..i];
                var j = i;
                while (j < expression.Length && char.IsWhiteSpace(expression[j])) j++;
                var isFunction = j < expression.Length && expression[j] == '(';
                if (!isFunction && !IsConstant(name)) names.Add(name.ToLowerInvariant());
            }
            else i++;
        }
        return names;
    }

    private static bool IsConstant(string name) => name.Equals("pi", StringComparison.OrdinalIgnoreCase) || name.Equals("e", StringComparison.OrdinalIgnoreCase);

    private sealed class Parser(string text, IReadOnlyDictionary<string, double> variables)
    {
        private int _pos;
        private int _depth;

        public void ExpectEnd()
        {
            SkipWhitespace();
            if (_pos != text.Length)
                throw new FormatException($"Unexpected '{text[_pos]}' at position {_pos + 1}.");
        }

        public double ParseExpression()
        {
            if (++_depth > 64) throw new FormatException("Formula is nested too deeply.");
            var value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+')) value += ParseTerm();
                else if (Match('-')) value -= ParseTerm();
                else break;
            }
            _depth--;
            return value;
        }

        private double ParseTerm()
        {
            var value = ParseUnary();
            while (true)
            {
                SkipWhitespace();
                if (Match('*')) value *= ParseUnary();
                else if (Match('/'))
                {
                    var divisor = ParseUnary();
                    if (divisor == 0) throw new FormatException("Division by zero.");
                    value /= divisor;
                }
                else if (Match('%'))
                {
                    var divisor = ParseUnary();
                    if (divisor == 0) throw new FormatException("Division by zero.");
                    value %= divisor;
                }
                else break;
            }
            return value;
        }

        private double ParseUnary()
        {
            SkipWhitespace();
            if (Match('+')) return ParseUnary();
            if (Match('-')) return -ParseUnary();
            return ParsePower();
        }

        private double ParsePower()
        {
            var baseValue = ParsePrimary();
            SkipWhitespace();
            if (Match('^'))
                return Math.Pow(baseValue, ParseUnary()); // right-associative
            return baseValue;
        }

        private double ParsePrimary()
        {
            SkipWhitespace();
            if (_pos >= text.Length) throw new FormatException("Unexpected end of formula.");

            if (Match('('))
            {
                var inner = ParseExpression();
                SkipWhitespace();
                if (!Match(')')) throw new FormatException("Missing ')'.");
                return inner;
            }

            var c = text[_pos];
            if (char.IsDigit(c) || c == '.')
                return ParseNumber();

            if (char.IsLetter(c) || c == '_')
            {
                var start = _pos;
                while (_pos < text.Length && (char.IsLetterOrDigit(text[_pos]) || text[_pos] == '_')) _pos++;
                var name = text[start.._pos];
                SkipWhitespace();
                if (Match('('))
                    return CallFunction(name.ToLowerInvariant(), ParseArguments());

                if (name.Equals("pi", StringComparison.OrdinalIgnoreCase)) return Math.PI;
                if (name.Equals("e", StringComparison.OrdinalIgnoreCase)) return Math.E;

                foreach (var (key, value) in variables)
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return value;
                throw new FormatException($"Unknown variable '{name}'.");
            }

            throw new FormatException($"Unexpected '{c}' at position {_pos + 1}.");
        }

        private List<double> ParseArguments()
        {
            var args = new List<double>();
            SkipWhitespace();
            if (Match(')')) return args;
            while (true)
            {
                args.Add(ParseExpression());
                SkipWhitespace();
                if (Match(')')) return args;
                if (!Match(',')) throw new FormatException("Expected ',' or ')'.");
            }
        }

        private static double CallFunction(string name, List<double> a)
        {
            double One() => a.Count == 1 ? a[0] : throw new FormatException($"{name}() takes one argument.");
            return name switch
            {
                "sqrt" => One() < 0 ? throw new FormatException("sqrt of a negative number.") : Math.Sqrt(a[0]),
                "abs" => Math.Abs(One()),
                "floor" => Math.Floor(One()),
                "ceil" => Math.Ceiling(One()),
                "exp" => Math.Exp(One()),
                "ln" or "log" => One() <= 0 ? throw new FormatException("log of a non-positive number.") : Math.Log(a[0]),
                "log10" => One() <= 0 ? throw new FormatException("log10 of a non-positive number.") : Math.Log10(a[0]),
                "sin" => Math.Sin(One()),
                "cos" => Math.Cos(One()),
                "tan" => Math.Tan(One()),
                "round" => a.Count switch
                {
                    1 => Math.Round(a[0], MidpointRounding.AwayFromZero),
                    2 => Math.Round(a[0], Math.Clamp((int)a[1], 0, 10), MidpointRounding.AwayFromZero),
                    _ => throw new FormatException("round() takes one or two arguments.")
                },
                "min" => a.Count >= 2 ? a.Min() : throw new FormatException("min() needs at least two arguments."),
                "max" => a.Count >= 2 ? a.Max() : throw new FormatException("max() needs at least two arguments."),
                "pow" => a.Count == 2 ? Math.Pow(a[0], a[1]) : throw new FormatException("pow() takes two arguments."),
                _ => throw new FormatException($"Unknown function '{name}'.")
            };
        }

        private double ParseNumber()
        {
            var start = _pos;
            while (_pos < text.Length && (char.IsDigit(text[_pos]) || text[_pos] == '.')) _pos++;
            if (_pos < text.Length && (text[_pos] == 'e' || text[_pos] == 'E')
                && _pos + 1 < text.Length && (char.IsDigit(text[_pos + 1]) || text[_pos + 1] is '+' or '-'))
            {
                _pos += 2;
                while (_pos < text.Length && char.IsDigit(text[_pos])) _pos++;
            }
            var token = text[start.._pos];
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? v
                : throw new FormatException($"Invalid number '{token}'.");
        }

        private bool Match(char c)
        {
            if (_pos < text.Length && text[_pos] == c) { _pos++; return true; }
            return false;
        }

        private void SkipWhitespace()
        {
            while (_pos < text.Length && char.IsWhiteSpace(text[_pos])) _pos++;
        }
    }
}
