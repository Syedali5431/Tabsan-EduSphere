namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>
/// SCORM 1.2 prerequisites (adlcp:prerequisites, AICC script): item identifiers combined with &amp; (and), | (or), ~ (not) and
/// parentheses; "id=status" / "id&lt;&gt;status" compare a status (passed, completed, failed, incomplete, browsed, not attempted
/// or their first letters); "n*{a,b,c}" holds when at least n of the set hold. A bare identifier holds when that item is
/// passed or completed. Unknown identifiers and unreadable expressions do not block the learner.
/// </summary>
public static class ScormPrerequisites
{
    /// <param name="statusOf">The lesson status of an item ("passed", "completed", "failed", "incomplete", "browsed", "not attempted"), or null when the identifier is unknown.</param>
    public static bool Met(string? expression, Func<string, string?> statusOf)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        try
        {
            var parser = new Parser(expression, statusOf);
            var result = parser.Or();
            return !parser.AtEnd || result;
        }
        catch (FormatException)
        {
            return true;
        }
    }

    private sealed class Parser(string text, Func<string, string?> statusOf)
    {
        private int _pos;

        public bool AtEnd { get { Skip(); return _pos >= text.Length; } }

        public bool Or()
        {
            var value = And();
            while (Peek('|')) { _pos++; value = And() | value; }
            return value;
        }

        private bool And()
        {
            var value = Unary();
            while (Peek('&')) { _pos++; value = Unary() & value; }
            return value;
        }

        private bool Unary()
        {
            if (Peek('~')) { _pos++; return !Unary(); }
            if (Peek('('))
            {
                _pos++;
                var value = Or();
                if (!Peek(')')) throw new FormatException();
                _pos++;
                return value;
            }
            Skip();
            var start = _pos;
            while (_pos < text.Length && char.IsDigit(text[_pos])) _pos++;
            if (_pos > start && Peek('*'))
            {
                var count = int.Parse(text[start.._pos]);
                _pos++;
                if (!Peek('{')) throw new FormatException();
                _pos++;
                var met = 0;
                do { if (Peek(',')) _pos++; if (Unary()) met++; } while (Peek(','));
                if (!Peek('}')) throw new FormatException();
                _pos++;
                return met >= count;
            }
            _pos = start;
            var id = Identifier();
            Skip();
            if (_pos + 1 < text.Length && text[_pos] == '<' && text[_pos + 1] == '>')
            {
                _pos += 2;
                return Status(id) != Normalize(Identifier());
            }
            if (Peek('='))
            {
                _pos++;
                return Status(id) == Normalize(Identifier());
            }
            var status = Status(id);
            return status is null or "passed" or "completed";
        }

        private string? Status(string id) => statusOf(id) is { } s ? Normalize(s) : null;

        private static string Normalize(string status) => status.Trim().ToLowerInvariant() switch
        {
            "p" => "passed",
            "c" => "completed",
            "f" => "failed",
            "i" => "incomplete",
            "b" => "browsed",
            "n" or "na" or "not attempted" => "not attempted",
            var s => s
        };

        private string Identifier()
        {
            Skip();
            var start = _pos;
            while (_pos < text.Length && !"&|~(){},=<> \t\r\n".Contains(text[_pos])) _pos++;
            if (_pos == start) throw new FormatException();
            return text[start.._pos];
        }

        private bool Peek(char c)
        {
            Skip();
            return _pos < text.Length && text[_pos] == c;
        }

        private void Skip()
        {
            while (_pos < text.Length && char.IsWhiteSpace(text[_pos])) _pos++;
        }
    }
}
