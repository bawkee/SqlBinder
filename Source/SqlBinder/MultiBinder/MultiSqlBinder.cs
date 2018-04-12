using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SqlBinder.Parsing;
using SqlBinder.Properties;

namespace SqlBinder.MultiBinder
{
	public class TemplateAlreadyDefinedException : Exception
	{
		public TemplateAlreadyDefinedException(string className, string templateName)
			: base(string.Format(Exceptions.TemplateAlreadyDefined, className, templateName)) { }
	}

	public class NoBestMatchException : Exception
	{
		public NoBestMatchException(string templateClass)
			: base(string.Format(Exceptions.NoBestMatch, templateClass)) { }
	}

	public class TemplateParserException : Exception
	{
		public TemplateParserException(string templateClass, Exception innerException)
			: base(string.Format(Exceptions.ParserFailureForTemplate, templateClass), innerException) { }
	}

	public class MultiSqlBinder : SqlBinder
	{
		private Dictionary<string, int> _matchingParams;
		private QueryParameter[] _paramsToMatch;
		private Dictionary<string, List<TemplateParameter>> _templateParams;
		private Template _processingTemplate;

		/// <summary>
		/// Gets a list of templates that will be processed when creating a query.
		/// </summary>
		public List<Template> Templates { get; internal set; } = new List<Template>();

		public MultiSqlBinder(IDbConnection dataConnection) : base(dataConnection)
		{
			//
		}

		private Template[] TemplatesByClass(string cls) => Templates.Where(t => string.CompareOrdinal(t.Class, cls) == 0).ToArray();

		/// <summary>
		/// Adds a template to the template collection which will be processed when creating
		/// queries. Template will be named "Default".
		/// </summary>
		/// <param name="className">The name of the template class/group to which this template belongs.</param>
		/// <param name="template">Contents of the template script.</param>
		public Template AddTemplate(string className, string template) => AddTemplate(className, "Default", template);

		/// <summary>
		/// Adds a template to the template collection which will be processed when creating
		/// queries.
		/// </summary>
		/// <param name="className">The name of the template class/group to which this template belongs.</param>
		/// <param name="name">The name of the template.</param>
		/// <param name="template">Contents of the template script.</param>
		public Template AddTemplate(string className, string name, string template)
		{
			var templ = Templates.FirstOrDefault(t => string.CompareOrdinal(t.Class, className) + string.CompareOrdinal(t.Name, name) == 0);

			if (templ != null)
				throw new TemplateAlreadyDefinedException(className, name);

			templ = new Template
			{
				Class = className,
				Name = name,
				Script = template
			};

			Templates.Add(templ);

			return templ;
		}

		/// <summary>
		/// Creates a <see cref="Query"/> based on provided template.
		/// </summary>
		public MultiQuery CreateQuery(Template template)
		{
			_templateParams = new Dictionary<string, List<TemplateParameter>>
			{
				{ template.Name, new List<TemplateParameter>() }
			};

			_paramsToMatch = null;

			var parser = new Parser();
			parser.RequestParameterValue += matchingParser_RequestParameterValue;
			PerformInitialParsing(parser, template);

			var templateParams = _templateParams[template.Name].ToArray(); // Params that were found in the script
			var queryParams = templateParams.Select(tp => new QueryParameter(tp.Name)).ToArray(); // Params that are required (which is in this case all of them, obviously)

			var query = new MultiQuery(this)
			{
				ClassTemplates = new[] { template },
				Template = template,
				SqlBinderScript = template.Script,
				QueryParameters = queryParams,
				TemplateParameters = templateParams
			};

			return query;
		}

		/// <summary>
		/// Creates a <see cref="Query"/> based on provided template class/group and desired parameter criteria. Best template will be chosen out of 
		/// the class/group based on given parameters.
		/// </summary>
		/// <param name="templateClass">Class of the previously defined templates.</param>
		/// <param name="parameters">List of parameters that will be matched against template parameters.</param>
		public MultiQuery CreateQuery(string templateClass, QueryParameter[] parameters)
		{
			//
			// Choose the best template according to parameters field and then
			// create a query with the chosen template.
			//
			_matchingParams = new Dictionary<string, int>();
			_paramsToMatch = parameters;

			var parser = new Parser();
			parser.RequestParameterValue += matchingParser_RequestParameterValue;

			_templateParams = new Dictionary<string, List<TemplateParameter>>();

			var templates = TemplatesByClass(templateClass);

			//
			// All templates must be registered first because templates can be used
			// in the template script itself.
			//
			foreach (var template in templates)
			{
				_matchingParams.Add(template.Name, 0);
				_templateParams.Add(template.Name, new List<TemplateParameter>());
			}

			//
			// Start parsing templates in order to find the best match
			//
			foreach (var template in templates)
				PerformInitialParsing(parser, template);


			if (_paramsToMatch != null)
			{
				if (_matchingParams.Values.Sum() <= 0 && _paramsToMatch.Length > 0)
					throw new NoBestMatchException(templateClass);
			}

			//
			// Get the best template from the result dictionary
			//
			var bestTemplateName = (from item in _matchingParams orderby item.Value descending select item.Key).First();
			var bestTemplate = templates.First(t => t.Name == bestTemplateName);

			var paramsReport = new StringBuilder();

			paramsReport.Append(string.Format(Resources.MatchesFoundPerTemplateForClass, templateClass));
			foreach (var pair in _matchingParams)
				paramsReport.Append(string.Format("{0}: {1}\r\n", pair.Key, pair.Value));
			paramsReport.Append(string.Format(Resources.TemplateChosen, bestTemplateName));

			OnDebugMessage(this, new ParserLogEventArgs
			{
				TemplateClass = templateClass,
				Text = paramsReport.ToString()
			});

			//
			// Finally, create the query
			//
			var query = new MultiQuery(this)
			{
				ClassTemplates = templates,
				Template = bestTemplate,
				SqlBinderScript = bestTemplate.Script,
				QueryParameters = parameters,
				TemplateParameters = _templateParams[bestTemplateName].ToArray()
			};

			return query;
		}

		private void PerformInitialParsing(Parser parser, Template template)
		{
			try
			{
				_processingTemplate = template;

				var pr = parser.Parse(template.Script);

				if (!pr.IsValid)
				{
					if (pr.CompileException != null)
						throw pr.CompileException;

					if (!string.IsNullOrEmpty(pr.Errors))
					{
						OnParserError(template, new ParserLogEventArgs
						{
							TemplateName = template.Name,
							TemplateClass = template.Class,
							Text = pr.Errors
						});

						if (ThrowScriptErrorException)
							throw new Exception(pr.Errors);
					}
				}

				if (!string.IsNullOrEmpty(pr.Warnings))
				{
					OnParserWarning(template, new ParserLogEventArgs
					{
						TemplateName = template.Name,
						TemplateClass = template.Class,
						Text = pr.Warnings
					});
				}
			}
			catch (Exception parseEx)
			{
				throw new TemplateParserException(template.Name, parseEx);
			}
		}

		private void matchingParser_RequestParameterValue(object sender, RequestParameterArgs e)
		{
			var used = false;

			if (_paramsToMatch == null)
				//
				// We're not matching stuff, every parameter is ok
				//
				used = true;
			else if (_paramsToMatch.Any(p => p.Name == e.Parameter.Name))
			{
				//
				// Parameter is matching
				//
				if (_matchingParams[_processingTemplate.Name] != -1)
					_matchingParams[_processingTemplate.Name]++;
				used = true;
			}
			else
			{
				//
				// Parameter does not match, if mandatory then ignore the template
				//
				if (e.Parameter.Flags.Any(p => p.ToUpper() == "MANDATORY"))
					_matchingParams[_processingTemplate.Name] = -1;
			}

			_templateParams[_processingTemplate.Name].Add(new TemplateParameter
			{
				Name = e.Parameter.Name,
				Flags = e.Parameter.Flags,
				IsUsed = used
			});

			if (used)
				e.Processed = true;
		}
	}
}
