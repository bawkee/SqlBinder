using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	public class BindVariableParameter : Parameter
	{
		public const string SYMBOLS = ":@?";

		public override string OpeningTag { get; }
		public override string ClosingTag { get => string.Empty; }

		internal BindVariableParameter(Token parent, Reader reader) 
			: base(parent) => OpeningTag = reader.Char.ToString();

		internal static bool Evaluate(Reader reader) 
			=> SYMBOLS.Contains(reader.Char) && char.IsLetterOrDigit(reader.Peek(1));

		internal override bool EvaluateClosingTag(Reader reader) 
			=> !(char.IsLetterOrDigit(reader.Char) || reader.Char == '_');

		public override string Name => ((ContentText)Content).Text;
	}
}
