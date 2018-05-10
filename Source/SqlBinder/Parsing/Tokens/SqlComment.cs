using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.Parsing.Tokens
{
	public class SqlComment : ContentToken
	{
		private const string OPENING_TAG = "/*";
		private const string CLOSING_TAG = "*/";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		internal SqlComment(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);
	}
}
