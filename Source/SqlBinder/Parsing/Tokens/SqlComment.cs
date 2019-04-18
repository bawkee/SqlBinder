using System.Linq;

namespace SqlBinder.Parsing.Tokens
{
	/// <summary>
	/// SQL comments, i.e. /* ... */.
	/// </summary>
	public class SqlComment : ContentToken
	{
		private const string OPENING_TAG = "/*";
		private const string CLOSING_TAG = "*/";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		internal SqlComment(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);
	}

	/// <summary>
	/// Inline SQL comments, i.e. --comment
	/// </summary>
	public class SqlInlineComment : ContentToken
	{
		private const string OPENING_TAG = "--";
		private const string LINEFEEDS = "\n\r";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => "";

		internal SqlInlineComment(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);

		internal override bool EvaluateClosingTag(Reader reader) => reader.EOF || LINEFEEDS.Contains(reader.Char);
	}
}
