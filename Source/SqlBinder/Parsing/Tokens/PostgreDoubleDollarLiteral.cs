namespace SqlBinder.Parsing.Tokens
{
    /// <summary>
    /// PostgreSQL literal, i.e. $$ ... $$ or $anyTag$ ... $anyTag$. It can contain any other characters inside. Note that dollar symbol
    /// may conflict with Informix EXEC keyword in which case you should optimize-away this token via Parser Hints.
    /// </summary>
    public class PostgreDoubleDollarLiteral : ContentToken
    {
        public const char SYMBOL = '$';
        public const int MAX_TAG = 256;

        public override string OpeningTag { get; }
        public override string ClosingTag { get; }

        internal PostgreDoubleDollarLiteral(Token parent, Reader reader)
            : base(parent)
        {
            OpeningTag = ClosingTag = DetermineTag(reader);
        }

        internal static bool Evaluate(Reader reader)
        {
            if (reader.Char != SYMBOL)
                return false;
            return DetermineTag(reader) != null;
        }

        private static unsafe string DetermineTag(Reader reader)
        {
            char* buf = stackalloc char[MAX_TAG];
            var done = false;

            for (var i = 0; i < MAX_TAG - 1; i++)
            {
                var c = reader.Peek(i);
                if (i > 0)
                {
                    if (c == SYMBOL)
                        done = true;
                    else if (!char.IsLetter(c))
                        break; // Invalid character
                }

                buf[i] = c;
                if (done)
                {
                    buf[i + 1] = '\0';
                    return new string(buf);
                }
            }

            return null;
        }
    }
}