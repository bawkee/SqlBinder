using System;
using SqlBinder.Properties;
using SqlBinder.Parsing.Tokens;

namespace SqlBinder.Parsing
{
	[Serializable]
	public class ParserException : Exception
	{
		public ParserException(Exception innerException) : base(Exceptions.ParserFailure, innerException) { }
		public ParserException(string errorMessage) : base(string.Format(Exceptions.ScriptNotValid, errorMessage)) { }
	}

	[Flags]
	public enum ParserHints
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
	/// Context-sensitive (back-referencing) lookahead SqlBinder script parser supporting T-SQL, MSSQL, MySql, PostgreSQL, and Oracle flavors. It scans through a 
	/// raw SqlBinder script and returns a parsed token tree representing the SqlBinder expression. This isn't SQL parser, it's SqlBinder parser. The only and only 
	/// aspects of SQL that it looks for are string literals and comments.
	/// Output tree can later be re-used by the template processor to produce many different queries. This parser has a rather simple implementation. It is supported 
	/// by a set of <see cref="Token"/> classes which themselves check the current character, perform look-aheads when needed and save the context information when 
	/// needed (such as opening tags for back-referencing). Any new trick in any future or existing SQL syntax which may interfere with the SqlBinder syntax may be 
	/// supported by simply adding another Token class. 
	/// </summary>
	public class SqlBinderParser
	{
		/// <summary>
		/// Various options that can be used to customize or optimize the parser.
		/// </summary>
		public ParserHints Hints { get; set; }

		private bool _doOracle;
		private bool _doPostgre;
		private bool _customParams;

		public SqlBinderParser() { }

		public SqlBinderParser(ParserHints hints) => Hints = hints;

		/// <summary>
		/// Parse the given SqlBinder script.
		/// </summary>
		public RootToken Parse(string sqlBinderTemplateScript)
		{
			var reader = new Reader(sqlBinderTemplateScript);
			var root = reader.Token;
			var openScopes = 0;

			_doOracle = !Hints.HasFlag(ParserHints.DisableOracleFlavors);
			_doPostgre = !Hints.HasFlag(ParserHints.DisablePostgreSqlFlavors);
			_customParams = Hints.HasFlag(ParserHints.UseCustomSyntaxForParams);

			ScopedToken scopedToken = null;

			while (reader.TryConsume())
			{
				// Prepare often-used references so boxing/unboxing is minimized
				scopedToken = reader.Token as ScopedToken ?? reader.Token.Parent as ScopedToken;
				var nestedToken = reader.Token as NestedToken ?? reader.Token.Parent as NestedToken;
				var contentToken = reader.Token as ContentToken ?? reader.Token.Parent as ContentToken;

				if (TryCloseScope(reader, scopedToken))
				{
					openScopes--;
					continue;
				}

				if (TryCreateScope(reader, nestedToken, contentToken))
				{
					openScopes++;
					continue;
				}

				if (reader.Token is TextToken textToken) // Keep writing on text elem
					textToken.Append(reader);
				else
					CreateTextToken(reader, contentToken, nestedToken);
			}

			if (TryCloseScope(reader, scopedToken))
				openScopes--;

			if (openScopes > 0)
				throw new ParserException(Exceptions.UnclosedScope);

			return root as RootToken;
		}

		private bool TryCloseScope(Reader reader, ScopedToken scopedToken)
		{
			if (scopedToken == null || !scopedToken.EvaluateClosingTag(reader))
				return false;
			reader.Consume(scopedToken.ClosingTag.Length - 1);
			reader.Token = scopedToken.Parent;
			return true;
		}

		private bool TryCreateScope(Reader reader, NestedToken nestedToken, ContentToken contentToken)
		{
			if (nestedToken == null || contentToken != null)
				return false;

			ScopedToken newElem;
			
			if (SqlBinderComment.Evaluate(reader))
				newElem = new SqlBinderComment(nestedToken);
			else if (SqlComment.Evaluate(reader))
				newElem = new SqlComment(nestedToken);
			else if (SqlInlineComment.Evaluate(reader))
				newElem = new SqlInlineComment(nestedToken);
			else if (SingleQuoteLiteral.Evaluate(reader))
				newElem = new SingleQuoteLiteral(nestedToken);
			else if (DoubleQuoteLiteral.Evaluate(reader))
				newElem = new DoubleQuoteLiteral(nestedToken);
			else if (_doOracle && OracleAQMLiteral.Evaluate(reader))
				newElem = new OracleAQMLiteral(nestedToken, reader);
			else if (_doPostgre && PostgreDoubleDollarLiteral.Evaluate(reader))
				newElem = new PostgreDoubleDollarLiteral(nestedToken, reader);
			else if (Scope.Evaluate(reader))
				newElem = new Scope(nestedToken, reader);
			else if (_customParams ? SqlBinderParameter.Evaluate(reader) : BindVariableParameter.Evaluate(reader))
				newElem = _customParams ? new SqlBinderParameter(nestedToken) as Parameter : new BindVariableParameter(nestedToken, reader);
			else
				return false;

			nestedToken.Children.Add(newElem);
			reader.Consume(newElem.OpeningTag.Length - 1);
			reader.Token = newElem;

			return true;
		}

		private void CreateTextToken(Reader reader, ContentToken contentToken, NestedToken nestedToken)
		{
			TextToken textToken;

			if (contentToken != null)
				contentToken.Content = textToken = contentToken.CreateContent();
			else if (nestedToken != null)
			{				
				if (ScopeSeparator.Evaluate(reader))
					textToken = new ScopeSeparator(reader.Token);
				else if (Sql.Evaluate(reader))
				{
					if (!Hints.HasFlag(ParserHints.UseCustomSyntaxForParams))
						textToken = new Sql(reader.Token);
					else
						textToken = new Sql(reader.Token, "[]");
				}
				else
					throw new NotSupportedException();

				nestedToken.Children.Add(textToken);					
			}
			else
				throw new InvalidOperationException();

			textToken.Append(reader);
			reader.Token = textToken;
		}
	}

	/// <summary>
	/// Text reader/scanner, used for basic string operations.
	/// </summary>
	internal class Reader
	{
		private bool _eof;

		public Reader(string buffer) => Buffer = buffer;

		public string Buffer { get; set; }

		public int Index { get; set; } = -1;

		public void Consume(int n = 1) => Index += n; // Optimistic

		public bool TryConsume() => !_eof && !(_eof = !(++Index < Buffer.Length)); // Pessimistic

		public char Char => _eof ? (char)0 : Buffer[Index];

		public Token Token { get; set; } = new RootToken();		

		public char Peek(int n = 0) => Index + n < Buffer.Length ? Buffer[Index + n] : (char)0;

		public bool EOF => _eof;

		public bool ScanFor(string str)
		{
			if (string.IsNullOrEmpty(str) || _eof)
				return false;
			for (var i = 0; i < str.Length; i++)
				if (Peek(i) != str[i])
					return false;
			return true;
		}

		private int _snapshot;

		public void StartSnapshot() => _snapshot = Index;

		public void FinishSnapshot() => Index = _snapshot;
	}
}
