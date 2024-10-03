using System.Linq;

namespace SqlBinder.Parsing.Tokens
{
    /// <summary>
    /// Sql is any text that was not translated into any other token. Sql can also support escape characters 
    /// which can be replaced with their respective character representation preventing from another token being recognized,
    /// e.g. [[parameter]] may be converted into '[parameter]' sql due to '[[' and ']]'.
    /// </summary>
    public class Sql : TextToken
    {
        public string EscapableSymbols { get; }

        internal Sql(Token parent) : base(parent)
        {
        }

        internal Sql(Token parent, string escapableSymbols)
            : base(parent) => EscapableSymbols = escapableSymbols;

        internal static bool Evaluate(Reader reader) => reader.Token is NestedToken;

        internal override void Append(Reader reader)
        {
            if (EscapableSymbols != null && EscapableSymbols.Contains(reader.Char))
                reader.Consume();
            base.Append(reader);
        }
    }
}