using System.Linq;

namespace SqlBinder.Parsing.Tokens
{
    /// <summary>
    /// Sql bind variable, guesses the syntax, i.e. :paramName or ?paramName or @paramName. These variables can be used
    /// to recognize SqlBinder parameters instead of the typical parameter syntax, namely <see cref="SqlBinderParameter"/> (i.e. '[paramName]').
    /// </summary>
    public class BindVariableParameter : Parameter
    {
        public const string SYMBOLS = ":@?";

        public override string OpeningTag { get; }

        public override string ClosingTag
        {
            get => string.Empty;
        }

        internal BindVariableParameter(Token parent, Reader reader)
            : base(parent) => OpeningTag = reader.Char.ToString();

        internal static bool Evaluate(Reader reader)
            => SYMBOLS.Contains(reader.Char) && char.IsLetterOrDigit(reader.Peek(1));

        internal override bool EvaluateClosingTag(Reader reader)
            => !(char.IsLetterOrDigit(reader.Char) || reader.Char == '_');

        public override string Name => ((ContentText)Content).Text;
    }
}