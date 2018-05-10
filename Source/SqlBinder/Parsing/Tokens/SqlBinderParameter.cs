using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	public class SqlBinderParameter : Parameter
	{
		public const string OPENING_SYMBOL = "[";
		public const string CLOSING_SYMBOL = "]";

		public override string OpeningTag => OPENING_SYMBOL;
		public override string ClosingTag => CLOSING_SYMBOL;

		internal SqlBinderParameter(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_SYMBOL[0])
		                                              && reader.Peek(1) != OPENING_SYMBOL[0] 
		                                              && reader.Peek(-1) != OPENING_SYMBOL[0];

		public override string Name => ((ContentText)Content).Text;
	}
}
