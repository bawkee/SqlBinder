using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SqlBinder.ConditionValues;
using SqlBinder.Parsing2;
using SqlBinder.Properties;

namespace SqlBinder
{
	[Serializable]
	public class ParserException : Exception
	{
		public ParserException(Exception innerException) : base(Exceptions.ParserFailure, innerException) { }
		public ParserException(string errorMessage) : base(string.Format(Exceptions.ScriptNotValid, errorMessage)) { }
	}

	[Serializable]
	public class UnmatchedConditionsException : Exception
	{
		public UnmatchedConditionsException(IEnumerable<Condition> conditions) : base(string.Format(Exceptions.NoMatchingParams,
			string.Join(", ", conditions.Select(c => c.Parameter).ToArray())))
		{ }
	}

	[Serializable]
	public class InvalidConditionException : Exception
	{
		public InvalidConditionException(ConditionValue value, Operator op, string message)
			: base(string.Format(Exceptions.InvalidCondition, value.GetType().Name, op, message)) { }
		public InvalidConditionException(ConditionValue value, Operator op, Exception innerException)
			: base(string.Format(Exceptions.InvalidCondition, value.GetType().Name, op, innerException.Message), innerException) { }
	}

	public class FormatParameterEventArgs : EventArgs
	{
		public string ParameterName { get; internal set; }
		public string FormattedName { get; set; }
	}

	public delegate void FormatParameterEventHandler(object sender, FormatParameterEventArgs e);

	/// <summary>
	/// Provides capability to parse and execute an SqlBinder scripts.
	/// </summary>
	public class Query
	{
	    public Query() { }

	    public Query(string script)
	    {
            SqlBinderScript = script;
	    }

        /// <summary>
        /// Occurs when parameter is to be formatted for the SQL ouput. Use this to specify custom parameter tags.
        /// </summary>
        public event FormatParameterEventHandler FormatParameterName;

		/// <summary>
		/// Gets or sets default parameter format string. See <see cref="FormatParameterName"/> event.
		/// </summary>
		protected virtual string DefaultParameterFormat { get; } = "{0}";

		private string FormatParameterNameInternal(string parameterName)
		{
			var e = new FormatParameterEventArgs
			{
				ParameterName = parameterName,
				FormattedName = string.Format(DefaultParameterFormat, parameterName)
			};
			OnFormatParameterName(this, e);
			return e.FormattedName;
		}

		/// <summary>
		/// Fires an event which can be used to format parameter names.
		/// </summary>
		protected virtual void OnFormatParameterName(object sender, FormatParameterEventArgs e) => FormatParameterName?.Invoke(sender, e);

		/// <summary>
		/// Gets or sets a value indicating whether script parser exceptions and errors should be thrown. True by default.
		/// </summary>
		public bool ThrowScriptErrorException { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether script parser exceptions and errors should be logged to debug output (True by default).
		/// </summary>
		public bool LogScriptErrorException { get; set; } = true;

		/// <summary>
		/// Gets or sets an SqlBinder script that was passed to this query.
		/// </summary>
		public string SqlBinderScript { get; set; }

		/// <summary>
		/// Gets the conditions which are required in order to build a valid query. There must be a parameter placeholder in your script for each condition.
		/// </summary>
		public List<Condition> Conditions { get; internal set; } = new List<Condition>();

		/// <summary>
		/// Gets or sets a collection of variables that will be passed onto the parser engine.
		/// </summary>
		public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Creates a condition for the query.
		/// </summary>
		/// <param name="parameterName">Name of the parameter for which this condition applies. It must match a placeholder in the script (i.e. '[parameterName]').</param>
		/// <param name="op">Condition operator.</param>
		/// <param name="value">Value of the condition. You can use your own or already predefined classes 
		/// such as <see cref="DateValue"/>, <see cref="NumberValue"/>, <see cref="StringValue"/> or <see cref="BoolValue"/>.</param>
		public virtual void SetCondition(string parameterName, Operator op, ConditionValue value)
		{
			if (parameterName == null)
				throw new ArgumentException(nameof(parameterName));

			RemoveCondition(parameterName);
			Conditions.Add(new Condition(parameterName, op, value));
		}

		/// <summary>
		/// Creates a <see cref="Condition"/> for the query.
		/// </summary>
		/// <param name="parameterName">Name of the parameter for which this condition applies. It must match a placeholder in the script (i.e. '[parameterName]').</param>
		/// <param name="value">Value of the condition. You can use your own or already predefined classes
		/// such as <see cref="DateValue"/>, <see cref="NumberValue"/>, <see cref="StringValue"/> or <see cref="BoolValue"/>.</param>
		public virtual void SetCondition(string parameterName, ConditionValue value)
		{
			SetCondition(parameterName, Operator.Is, value);
		}

		/// <summary>
		/// Translates a number-specific NumericOperator enum into a general purpose Operator enum.
		/// </summary>
		protected static Operator TranslateOperator(NumericOperator conditionOperator)
		{
			switch (conditionOperator)
			{
				case NumericOperator.IsNot: return Operator.IsNot;
				case NumericOperator.IsGreaterThan: return Operator.IsGreaterThan;
				case NumericOperator.IsGreaterThanOrEqualTo: return Operator.IsGreaterThanOrEqualTo;
				case NumericOperator.IsLessThan: return Operator.IsLessThan;
				case NumericOperator.IsLessThanOrEqualTo: return Operator.IsLessThanOrEqualTo;
				default: return Operator.Is;
			}
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue"/> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, decimal? from = null, decimal? to = null, bool inclusive = true)
		{
			var grthan = inclusive ? Operator.IsGreaterThanOrEqualTo : Operator.IsGreaterThan;
			var lessthan = inclusive ? Operator.IsLessThanOrEqualTo : Operator.IsLessThan;

			if (from.HasValue && to.HasValue)
			{
				if (!inclusive)
					throw new ArgumentException(Exceptions.SqlBetweenCanOnlyBeInclusive, nameof(inclusive));
				SetCondition(parameterName, Operator.IsBetween, new NumberValue(from.Value, to.Value));
			}
			else if (from.HasValue)
				SetCondition(parameterName, grthan, new NumberValue(from.Value));
			else if (to.HasValue)
				SetCondition(parameterName, lessthan, new NumberValue(to.Value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, decimal value, NumericOperator conditionOperator = NumericOperator.Is)
		{
			SetCondition(parameterName, TranslateOperator(conditionOperator), new NumberValue(value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, IEnumerable<decimal> values, bool isNot = false)
		{
			SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf, new NumberValue(values));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, int value, NumericOperator conditionOperator = NumericOperator.Is)
		{
			SetCondition(parameterName, TranslateOperator(conditionOperator), new NumberValue(value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, IEnumerable<int> values, bool isNot = false)
		{
			SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf, new NumberValue(values));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, long value, NumericOperator conditionOperator = NumericOperator.Is)
		{
			SetCondition(parameterName, TranslateOperator(conditionOperator), new NumberValue(value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, IEnumerable<long> values, bool isNot = false)
		{
			SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf, new NumberValue(values));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="DateValue"/> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, DateTime? from = null, DateTime? to = null, bool inclusive = true)
		{
			var grthan = inclusive ? Operator.IsGreaterThanOrEqualTo : Operator.IsGreaterThan;
			var lessthan = inclusive ? Operator.IsLessThanOrEqualTo : Operator.IsLessThan;

			if (from.HasValue && to.HasValue)
			{
				if (!inclusive)
					throw new ArgumentException(Exceptions.SqlBetweenCanOnlyBeInclusive, nameof(inclusive));
				SetCondition(parameterName, Operator.IsBetween, new DateValue(from.Value, to.Value));
			}
			else if (from.HasValue)
				SetCondition(parameterName, grthan, new DateValue(from.Value));
			else if (to.HasValue)
				SetCondition(parameterName, lessthan, new DateValue(to.Value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="DateValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, DateTime? value, NumericOperator conditionOperator = NumericOperator.Is)
		{
			SetCondition(parameterName, TranslateOperator(conditionOperator), new DateValue(value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="DateValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, IEnumerable<DateTime> values, bool isNot = false)
		{
			SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf, new DateValue(values));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="DateValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, bool value)
		{
			SetCondition(parameterName, Operator.Is, new BoolValue(value));
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="StringValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, string value, StringOperator conditionOperator = StringOperator.Is)
		{
			SetCondition(parameterName, TranslateStringOperator(conditionOperator), new StringValue(value));
		}

		/// <summary>
		/// Translates the string-specific StringOperator enum into a general purpose Operator enum.
		/// </summary>
		protected static Operator TranslateStringOperator(StringOperator conditionOperator)
		{
			switch (conditionOperator)
			{
				case StringOperator.IsLike: return Operator.Contains;
				case StringOperator.IsNotLike: return Operator.DoesNotContain;
				case StringOperator.IsNot: return Operator.IsNot;
				default: return Operator.Is;
			}
		}

		/// <summary>
		/// Creates a <see cref="Condition" /> with <see cref="StringValue" /> for the query.
		/// </summary>
		public virtual void SetCondition(string parameterName, IEnumerable<string> values, bool isNot = false)
		{
			SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf, new StringValue(values));
		}

		/// <summary>
		/// Returns a condition by its associated query parameter or null if not found.
		/// </summary>
		public virtual Condition GetCondition(string parameterName) => Conditions.FirstOrDefault(c => c.Parameter == parameterName);

		/// <summary>
		/// Removes a condition (if any) by its associated query parameter.
		/// </summary>
		public virtual void RemoveCondition(string parameterName) => Conditions.Remove(GetCondition(parameterName));

		/// <summary>
		/// Defines a user variable that can be used when this query is executed by the template parser engine.
		/// </summary>
		/// <param name="name">The name of the variable that will be matched in the script as '[variableName]'.</param>
		/// <param name="value">The value.</param>
		public virtual void DefineVariable(string name, object value) => Variables[name] = value;

		private readonly HashSet<Condition> _processedConditions = new HashSet<Condition>();

	    /// <summary>
	    /// A collection of SQL parameters that were produced after processing conditions.
	    /// </summary>
	    public Dictionary<string, object> SqlParameters { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// A resulting SQL produced by calling the method <see cref="GetSql"/>.
		/// </summary>
		public string OutputSql { get; set; }

		/// <summary>
		/// Parses the SqlBinder script, processes given conditons and returns the resulting SQL.
		/// </summary>
		/// <exception cref="ParserException">Thrown when the SqlBinder script is not valid. For example, when number of opening and closing []{} braces don't match.</exception>
		/// <exception cref="UnmatchedConditionsException">Thrown when there is a condition which wasn't found in the script. Mostly causes by mis-typed parameter 
		/// placeholders or condition names as they must be matched.</exception>
		/// <exception cref="InvalidConditionException">Thrown when some <see cref="ConditionValue"/> instance fails to generate the SQL.</exception>
		public string GetSql()
		{
			var parser = new Parser();

			parser.RequestParameterValue += Parser_RequestParameterValue;

			OutputSql = parser.Process(SqlBinderScript);

			var unprocessedConditions = Conditions.Except(_processedConditions).ToArray();
			if (unprocessedConditions.Any())
				throw new UnmatchedConditionsException(unprocessedConditions);

			return OutputSql;
		}

		/// <summary>
		/// Adds an SQL parameter to the collection. Use <see cref="Conditions"/> and <see cref="SetCondition(string, Operator, ConditionValue)"/> to set conditions which will
		/// add parameters automatically.
		/// </summary>
		public virtual void AddSqlParameter(string paramName, object paramValue)
		{
			SqlParameters[paramName] = paramValue;
		}

		private void Parser_RequestParameterValue(object sender, RequestParameterValueArgs e)
		{
			var cond = Conditions.FirstOrDefault(c => string.CompareOrdinal(c.Parameter, e.Parameter.Name) == 0);

			if (cond != null)
			{
				_processedConditions.Add(cond);

				var sql = ConstructParameterSql(cond.Operator, cond.Value, cond.Parameter);
				e.Value = sql;
			}
			else
			{
				var variableName = e.Parameter.Name;
				if (Variables.ContainsKey(variableName))
					e.Value = Variables[variableName].ToString();
			}
		}

		/// <summary>
		/// Compiles a parameter sql based on query parameter, operator and value.
		/// </summary>
		protected virtual string ConstructParameterSql(Operator sqlOperator, ConditionValue conditionValue, string parameterName)
		{
			try
			{
				var sql = conditionValue.GetSql(sqlOperator);

				if (string.IsNullOrEmpty(sql))
					throw new InvalidOperationException(Exceptions.EmptySqlReturned);

				var values = conditionValue.GetValues();

				if (values == null || values.Length <= 0)
					return sql;

				var paramsSql = new object[values.Length];
				var paramCnt = 1;

				// Create parameter(s) for each value
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];

					if (!(value is string) && value is IEnumerable valueEnumerable)
					{
						if (conditionValue.UseBindVariables)
						{
							// This value is enumerable (e.g. IN, NOT IN)
							var sqlParamNames = new List<string>();
							foreach (var subValue in valueEnumerable)
							{
								var sqlParamName = $"p{parameterName}_{paramCnt}";
								AddSqlParameter(sqlParamName, subValue);
								conditionValue.ProcessParameter(sqlParamName, subValue);
								sqlParamNames.Add(FormatParameterNameInternal(sqlParamName));
								paramCnt++;
							}

							paramsSql[i] = string.Join(", ", sqlParamNames.ToArray());
						}
						else
							paramsSql[i] = string.Join(", ", valueEnumerable);
					}
					else
					{
						if (conditionValue.UseBindVariables)
						{
							var sqlParamName = $"p{parameterName}_{paramCnt}";
							AddSqlParameter(sqlParamName, value);
							conditionValue.ProcessParameter(sqlParamName, value);
							paramsSql[i] = FormatParameterNameInternal(sqlParamName);
						}
						else
							paramsSql[i] = value;
					}

					paramCnt++;
				}

				sql = string.Format(sql, paramsSql);

				return sql;
			}
			catch (Exception ex)
			{
				throw new InvalidConditionException(conditionValue, sqlOperator, ex);
			}
		}
	}
}
