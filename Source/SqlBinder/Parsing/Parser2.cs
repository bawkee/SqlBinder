using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing2
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

		public string Process(NestedElement lexerRoot) => ConstructSql(ParseElement(lexerRoot)).ToString().Trim();

		public string Process(string sqlBinderScript) => Process(new Lexer().Process(sqlBinderScript));

		private ParsedElement ParseElement(Element lexerElement, ParsedElement parentElement = null, ParsedElement previousElement = null)
		{
			ParsedElement newElem = null;

			if (lexerElement is NestedElement nestedElement)
			{
				ParsedElement prevElemBuf = null;

				if (nestedElement is Scope scopeElement)
				{
					var parsedScope = newElem = new ParsedScope(scopeElement);
					parentElement?.Children.Add(parsedScope);
					foreach (var childElement in scopeElement.Children)
						prevElemBuf = ParseElement(childElement, parsedScope, prevElemBuf);
					parsedScope.IsValid = parsedScope.Children.OfType<ParsedScope>().Any(s => s.IsValid) ||
										  parsedScope.Children.OfType<ParsedParameter>().Any();
					if (!parsedScope.IsValid && previousElement?.LexerElement is ScopeSeparator)
						previousElement.IsValid = false;
				}
				else
				{
					var parsedChild = newElem = new ParsedElement(nestedElement);
					parentElement?.Children.Add(parsedChild);
					foreach (var childElement in nestedElement.Children)
						prevElemBuf = ParseElement(childElement, parsedChild, prevElemBuf);
				}
			}
			else if (lexerElement is SqlBinderComment)
				return previousElement;
			else if (lexerElement is Parameter parameterElement)
			{
				var parsedParam = (ParsedParameter) (newElem = ParseParameter(parameterElement));
				if (!string.IsNullOrEmpty(parsedParam.ConditionSql))
					parentElement.Children.Add(parsedParam);
			}
			else if (lexerElement is ContentElement contentElement)
			{
				var parsedContent = newElem = new ParsedElement(contentElement);
				parentElement?.Children.Add(parsedContent);
				ParseElement(contentElement.Content, parsedContent);
			}
			else
				parentElement?.Children.Add(newElem = new ParsedElement(lexerElement));

			return newElem;
		}

		private ParsedParameter ParseParameter(Parameter parameter)
		{
			var args = new RequestParameterValueArgs {Parameter = parameter};
			OnRequestParameterValue(args);
			return new ParsedParameter(parameter, args.Value);
		}

		private static StringBuilder ConstructSql(ParsedElement element, StringBuilder buffer = null)
		{
			if (buffer == null)
				buffer = new StringBuilder(BUFFER_CAPACITY);

			if (!element.IsValid)
				return buffer;
			
			if (element is ParsedParameter parsedParameter)
				buffer.Append(parsedParameter.ConditionSql);
			else switch (element.LexerElement)
			{
				case Sql sql:
					buffer.Append(sql.Text);
					break;
				case ScopeSeparator separator:
					buffer.Append(separator.Text);
					buffer.Append("AND");
					buffer.Append(' ');
					break;
				case ContentElement contentElement:
					buffer.Append(contentElement.OpeningTag);
					ConstructSql(element.Children.First(), buffer);
					buffer.Append(contentElement.ClosingTag);
					break;
				case TextElement textElement:
					buffer.Append(textElement.Text);
					break;
				case NestedElement _:
					foreach (var elementChild in element.Children)
						ConstructSql(elementChild, buffer);
					break;
				default:
					throw new NotSupportedException();
			}

			return buffer;
		}
	}

	public class ParsedElement
	{
		public List<ParsedElement> Children { get; } = new List<ParsedElement>();
		public Element LexerElement { get; }		
		public ParsedElement(Element lexerElement) => LexerElement = lexerElement;
		public bool IsValid { get; set; } = true;
	}

	public class ParsedScope : ParsedElement
	{		
		public ParsedScope(Scope lexerScope) : base(lexerScope) { }
	}

	public class ParsedParameter : ParsedElement
	{
		public string ConditionSql { get; }

		public ParsedParameter(Parameter lexerParameter, string sql) : base(lexerParameter)
		{
			ConditionSql = sql;
		}
	}
}
