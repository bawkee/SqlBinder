using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.Parsing.Tokens
{
	/// <summary>
	/// Sql string literal, i.e. ' ... '. It can contain other literals inside. It is also escapable, i.e. '' and \' are acceptable.
	/// </summary>
	public class SingleQuoteLiteral : EscapableStringLiteral
	{
		public const string SYMBOL = "'";

		public override string OpeningTag => SYMBOL;
		public override string ClosingTag => SYMBOL;

		public override char LiteralTag => SYMBOL[0];

		internal SingleQuoteLiteral(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => Evaluate(reader, SYMBOL[0]);

		public override TextToken CreateContent() => new EscapableContentText(this, SYMBOL[0]);
	}
}
