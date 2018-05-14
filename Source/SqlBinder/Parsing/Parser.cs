using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlBinder.Parsing.Tokens;

namespace SqlBinder.Parsing
{
	public class RequestParameterValueArgs : EventArgs
	{
		public Parameter Parameter { get; internal set; }
		public string Value { get; set; }
	}

	public delegate void RequestParameterValueEventHandler(object sender, RequestParameterValueArgs e);

	public class Parser
	{
		public const int BUFFER_CAPACITY = 4096;

		public event RequestParameterValueEventHandler RequestParameterValue;		

		protected virtual void OnRequestParameterValue(RequestParameterValueArgs e) => RequestParameterValue?.Invoke(this, e);

		public string Parse(NestedToken lexerRoot) => ConstructSql(ParseToken(lexerRoot)).ToString().Trim();

		public string Parse(string sqlBinderScript) => Parse(new Lexer().Tokenize(sqlBinderScript));

		private ParsedToken ParseToken(Token lexerToken, ParsedToken parentToken = null, ParsedToken previousSibling = null)
		{
			ParsedToken newElem = null;

			if (lexerToken is NestedToken nestedToken)
			{
				ParsedToken prevElemBuf = null;

				if (nestedToken is Scope scopeToken)
				{
					var parsedScope = newElem = new ParsedScope(scopeToken);
					parentToken?.Children.Add(parsedScope);
					foreach (var childToken in scopeToken.Children)
						prevElemBuf = ParseToken(childToken, parsedScope, prevElemBuf);
					// Validate this scope
					parsedScope.IsValid = parsedScope.Children.OfType<ParsedScope>().Any(s => s.IsValid) ||
										  parsedScope.Children.OfType<ParsedParameter>().Any();
					// Validate scope separator that came before it
					if (previousSibling?.LexerToken is ScopeSeparator)
						previousSibling.IsValid = parsedScope.IsValid && (parentToken?.Children.OfType<ParsedScope>().Any(s => s.IsValid && s != parsedScope) ?? false);
				}
				else
				{
					var parsedChild = newElem = new ParsedToken(nestedToken);
					parentToken?.Children.Add(parsedChild);
					foreach (var childToken in nestedToken.Children)
						prevElemBuf = ParseToken(childToken, parsedChild, prevElemBuf);
				}
			}
			else if (lexerToken is SqlBinderComment)
				return previousSibling;
			else if (lexerToken is Parameter parameterToken)
			{
				var parsedParam = (ParsedParameter) (newElem = ParseParameter(parameterToken));
				if (!string.IsNullOrEmpty(parsedParam.ConditionSql))
					parentToken.Children.Add(parsedParam);
			}
			else if (lexerToken is ContentToken contentToken)
			{
				var parsedContent = newElem = new ParsedToken(contentToken);
				parentToken?.Children.Add(parsedContent);
				ParseToken(contentToken.Content, parsedContent);
			}
			else
				parentToken?.Children.Add(newElem = new ParsedToken(lexerToken));

			return newElem;
		}

		private ParsedParameter ParseParameter(Parameter parameter)
		{
			var args = new RequestParameterValueArgs {Parameter = parameter};
			OnRequestParameterValue(args);
			return new ParsedParameter(parameter, args.Value);
		}

		private static StringBuilder ConstructSql(ParsedToken token, StringBuilder buffer = null)
		{
			if (buffer == null)
				buffer = new StringBuilder(BUFFER_CAPACITY);

			if (!token.IsValid)
				return buffer;
			
			if (token is ParsedParameter parsedParameter)
				buffer.Append(parsedParameter.ConditionSql);
			else switch (token.LexerToken)
			{
				case Sql sql:
					buffer.Append(sql.Text);
					break;
				case ScopeSeparator separator:
					var sep = "AND";
					if (separator.Parent is Scope parentScope && parentScope.Flags.Contains('@'))
						sep = "OR";
					buffer.Append(separator.Text);
					buffer.Append(sep);
					buffer.Append(' ');
					break;
				case ContentToken contentToken:
					buffer.Append(contentToken.OpeningTag);
					ConstructSql(token.Children.First(), buffer);
					buffer.Append(contentToken.ClosingTag);
					break;
				case TextToken textToken:
					buffer.Append(textToken.Text);
					break;
				case NestedToken _:
					foreach (var childToken in token.Children)
						ConstructSql(childToken, buffer);
					break;
				default:
					throw new NotSupportedException();
			}

			return buffer;
		}
	}

	public class ParsedToken
	{
		public List<ParsedToken> Children { get; } = new List<ParsedToken>();
		public Token LexerToken { get; }
		internal ParsedToken(Token lexerToken) => LexerToken = lexerToken;
		public bool IsValid { get; set; } = true;
	}

	public class ParsedScope : ParsedToken
	{
		internal ParsedScope(Scope lexerScope) : base(lexerScope) { }
	}

	public class ParsedParameter : ParsedToken
	{
		public string ConditionSql { get; }

		internal ParsedParameter(Parameter lexerParameter, string sql) : base(lexerParameter) => ConditionSql = sql;
	}
}
