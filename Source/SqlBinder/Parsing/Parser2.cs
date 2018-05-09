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

		public string Process(NestedElement lexerRoot) => ConstructSql(ParseElement(lexerRoot)).ToString();

		private ParsedElement ParseElement(Element lexerElement, ParsedElement parsedElement = null)
		{
			if (parsedElement == null)
			{
				if (lexerElement is NestedElement)
					parsedElement = new ParsedElement(lexerElement);
				else
					throw new ArgumentException(nameof(parsedElement));
			}

			if (lexerElement is NestedElement nestedElement)
			{
				if (nestedElement is Scope scopeElement)
				{
					var parsedScope = new ParsedScope(scopeElement);
					parsedElement.Children.Add(parsedScope);
					foreach (var childElement in scopeElement.Children)
						ParseElement(childElement, parsedScope);
					parsedScope.IsValid = parsedScope.Children.OfType<ParsedScope>().Any(s => s.IsValid);
				}
				else
				{
					var parsedChild = new ParsedElement(nestedElement);
					parsedElement.Children.Add(parsedChild);
					foreach (var childElement in nestedElement.Children)
						ParseElement(childElement, parsedChild);
				}
			}
			else if (lexerElement is Parameter parameterElement)
			{
				var parsedParam = ParseParameter(parameterElement);
				if (string.IsNullOrEmpty(parsedParam.ConditionSql))
					((ParsedScope) parsedElement).IsValid = false;
				else
					parsedElement.Children.Add(parsedParam);
			}
			else if (lexerElement is ContentElement contentElement)
			{
				var parsedContent = new ParsedElement(contentElement);
				parsedElement.Children.Add(parsedContent);
				ParseElement(contentElement.Content, parsedContent);
			}
			else
				parsedElement.Children.Add(new ParsedElement(lexerElement));

			return parsedElement;
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

			if (element.LexerElement is SqlBinderComment)
				return buffer;
			
			if (element is ParsedParameter parsedParameter)
				buffer.Append(parsedParameter.ConditionSql);
			else switch (element.LexerElement)
			{
				case Sql sql:
					buffer.Append(sql.Text);
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
		//public ParsedElement Parent { get; set; }
		public Element LexerElement { get; }		
		public ParsedElement(Element lexerElement) => LexerElement = lexerElement;
	}

	public class ParsedScope : ParsedElement
	{
		public bool IsValid { get; set; } = true;
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
