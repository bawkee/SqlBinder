using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlBinder.Properties;
using SqlBinder.Parsing.Tokens;

namespace SqlBinder.Parsing
{
	[Serializable]
	public class LexerException : Exception
	{
		public LexerException(Exception innerException) : base(Exceptions.ParserFailure, innerException) { }
		public LexerException(string errorMessage) : base(string.Format(Exceptions.ScriptNotValid, errorMessage)) { }
	}

	[Flags]
	public enum LexerHints
	{
		None = 0,
		/// <summary>
		/// Improves performance slightly if you never plan on using the parser for PostgreSQL.
		/// </summary>
		DisablePostgreSqlFlavors = 1,
		/// <summary>
		/// Improves performance slightly if you never plan on using the parser for Oracle.
		/// </summary>
		DisableOracleFlavors = 2,

		/// <summary>
		/// Instead of using standard parameter syntax (:param, @param or ?param) as SqlBinder condition placeholders, custom syntax will be used,
		/// (i.e. [Parameter Name]). This allows you to mix Ado.Net parameters with SqlBinder conditions or use special characters for parameter 
		/// names. If you set this and want to use the [...] syntax in MSSQL you will have to escape the brackets (i.e. [[...]]).
		/// </summary>
		UseCustomSyntaxForParams = 4
	}

	/// <summary>
	/// Scans through a raw SqlBinder script and creates an object/token tree representing the SQL expression. It's a more than just a 
	/// tokenizer since it builds a hierarchy while scanning. This provides flexibility when facing various custom SQL syntaxes that SqlBinder
	/// shouldn't scan, like PostgreSQL, MySQL or Oracle AQM literals. The lexer will be aware of the context it's in and not do redundant
	/// scans that would later have to be thrown out by the parser. The idea is to do as much as possible at this point and cache it for
	/// many later uses.
	/// </summary>
	public class Lexer
	{
		/// <summary>
		/// Various options that can be used to customize or optimize the lexer.
		/// </summary>
		public LexerHints Hints { get; set; }

		public Lexer() { }

		public Lexer(LexerHints hints) => Hints = hints;

		/// <summary>
		/// Tokenize the given SqlBinder script.
		/// </summary>
		public RootToken Tokenize(string script)
		{
			var reader = new Reader(script);
			var root = reader.Token;
			var openScopes = 0;

			while (reader.Read())
			{
				// Prepare often-used references so boxing/unboxing is minimized
				var scopedToken = reader.Token as ScopedToken ?? reader.Token.Parent as ScopedToken;
				var nestedToken = reader.Token as NestedToken ?? reader.Token.Parent as NestedToken;
				var contentToken = reader.Token as ContentToken ?? reader.Token.Parent as ContentToken;

				if (TryToCloseScope(reader, scopedToken))
				{
					openScopes--;
					continue;
				}

				if (TryToCreateScope(reader, nestedToken, contentToken))
				{
					openScopes++;
					continue;
				}

				if (reader.Token is TextToken token) // Keep writing on text elem
					token.Append(reader.Char);
				else
					CreateTextToken(reader, contentToken, nestedToken);
			}

			if (openScopes > 0)
				throw new LexerException(Exceptions.UnclosedScope);

			return root as RootToken;
		}

		private bool TryToCloseScope(Reader reader, ScopedToken scopedToken)
		{
			if (scopedToken == null || !scopedToken.EvaluateClosingTag(reader))
				return false;
			reader.Advance(scopedToken.ClosingTag.Length - 1);
			reader.Token = scopedToken.Parent;
			return true;
		}

		private bool TryToCreateScope(Reader reader, NestedToken nestedToken, ContentToken contentToken)
		{
			if (nestedToken == null || contentToken != null)
				return false;

			ScopedToken newElem;
			
			if (SqlBinderComment.Evaluate(reader)) newElem = new SqlBinderComment(nestedToken);
			else if (SqlComment.Evaluate(reader)) newElem = new SqlComment(nestedToken);
			else if (SingleQuoteLiteral.Evaluate(reader)) newElem = new SingleQuoteLiteral(nestedToken);
			else if (DoubleQuoteLiteral.Evaluate(reader)) newElem = new DoubleQuoteLiteral(nestedToken);
			else if (!Hints.HasFlag(LexerHints.DisableOracleFlavors) && OracleAQMLiteral.Evaluate(reader))
				newElem = new OracleAQMLiteral(nestedToken, reader);
			else if (!Hints.HasFlag(LexerHints.DisablePostgreSqlFlavors) && PostgreDoubleDollarLiteral.Evaluate(reader))
				newElem = new PostgreDoubleDollarLiteral(nestedToken, reader);
			else if (Scope.Evaluate(reader)) newElem = new Scope(nestedToken, reader);
			else if (Hints.HasFlag(LexerHints.UseCustomSyntaxForParams) ? 
				SqlBinderParameter.Evaluate(reader) : BindVariableParameter.Evaluate(reader))
				newElem = Hints.HasFlag(LexerHints.UseCustomSyntaxForParams) ? 
					new SqlBinderParameter(nestedToken) as Parameter : new BindVariableParameter(nestedToken, reader);
			else return false;

			nestedToken.Children.Add(newElem);
			reader.Advance(newElem.OpeningTag.Length - 1);
			reader.Token = newElem;

			return true;
		}

		private void CreateTextToken(Reader reader, ContentToken contentToken, NestedToken nestedToken)
		{
			TextToken newElem;

			if (ContentText.Evaluate(reader)) newElem = new ContentText(reader.Token);
			else if (ScopeSeparator.Evaluate(reader)) newElem = new ScopeSeparator(reader.Token);
			else if (Sql.Evaluate(reader))
			{
				if (!Hints.HasFlag(LexerHints.UseCustomSyntaxForParams))
					newElem = new Sql(reader.Token);
				else
				{
					const string ESCAPABLE_SQL_SYMBOLS = "[]";
					newElem = new Sql(reader.Token, ESCAPABLE_SQL_SYMBOLS);
				}
			}
			else throw new NotSupportedException();

			if (contentToken != null)
				contentToken.Content = newElem;
			else if (nestedToken != null)
				nestedToken.Children.Add(newElem);
			else
				throw new InvalidOperationException();

			newElem.Append(reader.Char);
			reader.Token = newElem;
		}
	}

	/// <summary>
	/// Text reader/scanner, used for basic string operations.
	/// </summary>
	internal class Reader
	{		
		public Reader(string buffer) => Buffer = buffer;
		public string Buffer { get; set; }
		public int Index { get; set; } = -1;
		public void Advance(int n) => Index += n;
		public char Char => Buffer[Index];
		public Token Token { get; set; } = new RootToken();
		public bool Read() => ++Index < Buffer.Length;
		public char Peek(int n = 0) => Index + n < Buffer.Length ? Buffer[Index + n] : (char)0;

		public bool TestString(string str)
		{			
			if (string.IsNullOrEmpty(str))
				return false;
			for (var i = 0; i < str.Length; i++)
				if (Peek(i) != str[i])
					return false;
			return true;
		}
	}
}
