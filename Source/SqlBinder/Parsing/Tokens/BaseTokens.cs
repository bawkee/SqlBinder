using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	//* ---------------------------------- */
	// Basic token types:
	// 
	// Token 
	//		TextToken : Token
	//		ScopedToken : Token
	//			ContentToken : ScopedToken
	//			NestedToken : ScopedToken
	//* ---------------------------------- */

	/// <summary>
	/// Token base class.
	/// </summary>
	public abstract class Token
	{
		public Token Parent { get; internal set; }
	}

	/// <summary>
	/// Token that contains string content.
	/// </summary>
	public abstract class TextToken : Token
	{
		public const int BUFFER_SIZE = 512;

		protected TextToken(Token parent) => Parent = parent;

		protected StringBuilder Buffer { get; set; } = new StringBuilder(BUFFER_SIZE);

		private string _text;
		public string Text => _text ?? (_text = Buffer.ToString());

		internal virtual void Append(Reader reader)
		{
			Buffer.Append(reader.Char);
			_text = null;
		}

		public override string ToString() => $"{GetType().Name} ({Text})";
	}

	/// <summary>
	/// Token that contains opening and closing tags. This is an abstract class to be used for tokens that
	/// contain other token(s) or text. 
	/// </summary>
	public abstract class ScopedToken : Token
	{
		protected ScopedToken(Token parent) => Parent = parent;

		public abstract string OpeningTag { get; }
		public abstract string ClosingTag { get; }

		internal static bool Evaluate(Reader reader, string tag) => reader.ScanFor(tag);
		internal static bool Evaluate(Reader reader, char tag) => reader.Peek() == tag;
		internal virtual bool EvaluateClosingTag(Reader reader) => Evaluate(reader, ClosingTag);
	}

	/// <summary>
	/// A token which encloses another token with opening and closing tags.
	/// </summary>
	public abstract class ContentToken : ScopedToken
	{
		public virtual TextToken Content { get; internal set; }

		public virtual TextToken CreateContent() => new ContentText(this);

		protected ContentToken(Token parent) : base(parent) { }

		public override string ToString() => $"{GetType().Name} ({Content})";
	}

	/// <summary>
	/// A token which encloses one or more other tokens with opening and closing tags.
	/// </summary>
	public abstract class NestedToken : ScopedToken
	{
		public List<Token> Children { get; protected set; } = new List<Token>();

		protected NestedToken(Token parent) : base(parent) { }

		public override string ToString() => $"{GetType().Name} ({Children.Count} children)";
	}

	/// <summary>
	/// Root token which encloses everything else
	/// </summary>
	public class RootToken : NestedToken
	{
		internal RootToken() : base(null) { }

		public override string OpeningTag => null;
		public override string ClosingTag => null;
	}

	/// <summary>
	/// Token that represents string contents inside other <see cref="ContentToken"/> tokens.
	/// </summary>
	public class ContentText : TextToken
	{
		internal ContentText(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader) => reader.Token is ContentToken;
	}

	/// <summary>
	/// Token representing <see cref="EscapableStringLiteral"/> token's content.
	/// </summary>
	public class EscapableContentText : ContentText
	{
		public char EscapableSymbol { get; }

		internal EscapableContentText(Token parent, char symbol)
			: base(parent) => EscapableSymbol = symbol;

		internal override void Append(Reader reader)
		{
			if (reader.Char == EscapableSymbol && reader.Peek(1) == EscapableSymbol ||
			    reader.Char == '\\' && reader.Peek(1) == EscapableSymbol)
			{
				base.Append(reader);
				reader.Consume();
			}
			base.Append(reader);
		}
	}

	/// <summary>
	/// Content Token representing string literals which can be escaped by backslash (\) or by repeating their tags.
	/// </summary>
	public abstract class EscapableStringLiteral : ContentToken
	{
		protected EscapableStringLiteral(Token parent) : base(parent) { }

		internal override bool EvaluateClosingTag(Reader reader) =>
			base.EvaluateClosingTag(reader) && reader.Peek(1) != LiteralTag;

		public abstract char LiteralTag { get; }
	}

	/// <summary>
	/// Token representing an SqlBinder parameter.
	/// </summary>
	public abstract class Parameter : ContentToken
	{
		protected Parameter(Token parent) : base(parent) { }

		public abstract string Name { get; }
	}
}
