using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	public class SqlBinderComment : ContentToken
	{
		private const string OPENING_TAG = "/*{";
		private const string CLOSING_TAG = "}*/";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		internal SqlBinderComment(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);
	}
}
