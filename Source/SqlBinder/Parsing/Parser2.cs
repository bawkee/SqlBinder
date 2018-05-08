using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.Parsing2
{
	public class Parser
	{
		public const int BUFFER_CAPACITY = 4096;

		public StringBuilder Buffer = new StringBuilder(BUFFER_CAPACITY);

		private bool ProcessElement(Element element)
		{
			var ret = true;

			if (element is SqlBinderComment)
				return false;

			if (element is Sql sql)
				Buffer.Append(sql.Text);
			else if (element is Parameter)
				return false; // TODO
			else if (element is ContentElement contentElement)
			{
				Buffer.Append(contentElement.OpeningTag);
				ProcessElement(contentElement.Content);
				Buffer.Append(contentElement.ClosingTag);
			}
			else if (element is TextElement textElement)
				Buffer.Append(textElement.Text);
			else if (element is Scope scope)
			{
				if (!scope.Children.Any(s => s is Parameter || s is Scope))
					return false;
			}

			if (element is NestedElement nestedElement)
			{
				ret = false;
				foreach (var nestedElementChild in nestedElement.Children)
					ret |= ProcessElement(nestedElementChild);
			}

			return ret;
		}
	}
}
