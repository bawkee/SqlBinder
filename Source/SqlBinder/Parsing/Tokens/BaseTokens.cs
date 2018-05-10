using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	///* ----------------------- */
	/// Basic token types:
	/// 
	/// Token
	///		TextToken
	///		ScopedToken
	///			ContentToken
	///			NestedToken
	///* ----------------------- */

	public abstract class Token
	{
		public Token Parent { get; internal set; }
	}

	public abstract class TextToken : Token
	{
		public const int BUFFER_SIZE = 512;

		protected TextToken(Token parent) => Parent = parent;

		protected StringBuilder Buffer { get; set; } = new StringBuilder(BUFFER_SIZE);

		private string _text;
		public string Text => _text ?? (_text = Buffer.ToString());

		internal virtual void Append(char c)
		{
			Buffer.Append(c);
			_text = null;
		}

		public override string ToString() => $"{GetType().Name} ({Text})";
	}

	public abstract class ScopedToken : Token
	{
		protected ScopedToken(Token parent) => Parent = parent;

		public abstract string OpeningTag { get; }
		public abstract string ClosingTag { get; }

		internal static bool Evaluate(Reader reader, string tag) => reader.TestString(tag);
		internal static bool Evaluate(Reader reader, char tag) => reader.Peek() == tag;
		internal virtual bool EvaluateClosingTag(Reader reader) => Evaluate(reader, ClosingTag);
	}

	public abstract class ContentToken : ScopedToken
	{
		public Token Content { get; internal set; }

		protected ContentToken(Token parent) : base(parent) { }

		public override string ToString() => $"{GetType().Name} ({Content})";
	}

	public abstract class NestedToken : ScopedToken
	{
		public List<Token> Children { get; protected set; } = new List<Token>();

		protected NestedToken(Token parent) : base(parent) { }

		public override string ToString() => $"{GetType().Name} ({Children.Count} children)";
	}

	public class RootToken : NestedToken
	{
		internal RootToken() : base(null) { }

		public override string OpeningTag => null;
		public override string ClosingTag => null;
	}

	public class ContentText : TextToken
	{
		internal ContentText(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => reader.Token is ContentToken;
	}

	public abstract class EscapableStringLiteral : ContentToken
	{
		protected EscapableStringLiteral(Token parent) : base(parent) { }

		private bool _skip;

		internal override bool EvaluateClosingTag(Reader reader)
		{
			if (_skip)
				return _skip = false;
			if (!base.EvaluateClosingTag(reader))
				return false;
			if (Content != null && reader.Peek(-1) == '\\')
				return false;
			if (reader.Peek(1) != LiteralTag)
				return true;
			_skip = true;
			return false;
		}

		public abstract char LiteralTag { get; }
	}

	public abstract class Parameter : ContentToken
	{
		protected Parameter(Token parent) : base(parent) { }

		public abstract string Name { get; }
	}
}
