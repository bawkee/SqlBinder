using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SqlBinder.ConditionValues;
using SqlBinder.Parsing;
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
	public abstract class QueryBase<TConnection, TCommand> 
		where TConnection: class, IDbConnection 
		where TCommand : class, IDbCommand
	{
		protected QueryBase(TConnection connection)
		{
			DataConnection = connection;
		}

		protected QueryBase(TConnection connection, string script)
			: this(connection)
		{
			SqlBinderScript = script;
		}

		/// <summary>
		/// Basic, assumed, ADO.Net type mappings. This can be overriden on any level (Query, ConditionValue).
		/// </summary>
		private Dictionary<Type, DbType> _dbTypeMap { get; } =
			new Dictionary<Type, DbType>
			{
				[typeof(byte)] = DbType.Byte,
				[typeof(sbyte)] = DbType.SByte,
				[typeof(short)] = DbType.Int16,
				[typeof(ushort)] = DbType.UInt16,
				[typeof(int)] = DbType.Int32,
				[typeof(uint)] = DbType.UInt32,
				[typeof(long)] = DbType.Int64,
				[typeof(ulong)] = DbType.UInt64,
				[typeof(float)] = DbType.Single,
				[typeof(double)] = DbType.Double,
				[typeof(decimal)] = DbType.Decimal,
				[typeof(bool)] = DbType.Boolean,
				[typeof(string)] = DbType.String,
				[typeof(char)] = DbType.StringFixedLength,
				[typeof(Guid)] = DbType.Guid,
				[typeof(DateTime)] = DbType.DateTime,
				[typeof(DateTimeOffset)] = DbType.DateTimeOffset,
				[typeof(byte[])] = DbType.Binary
			};

		/// <summary>
		/// When overriden in a derived class it allows customizing ADO DbType guessing based on clr types.
		/// </summary>
		protected virtual DbType OnResolveDbType(Type clrType)
		{
			var clrTypeNN = Nullable.GetUnderlyingType(clrType) ?? clrType;
			return _dbTypeMap.ContainsKey(clrTypeNN) ? _dbTypeMap[clrTypeNN] : DbType.Object;
		}

		/// <summary>
		/// Gets or sets default parameter format string for all queries. See <see cref="FormatParameterName"/> event.
		/// </summary>
		protected virtual string DefaultParameterFormat { get; } = "{0}";

		/// <summary>
		/// Occurs when parameter is to be formatted for the SQL ouput. Use this to specify custom parameter tags for your DBMS.
		/// </summary>
		public event FormatParameterEventHandler FormatParameterName;

		/// <summary>
		/// Fires an event which can be used to format all or some queries and can be overriden in a derived class.
		/// </summary>
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

		protected virtual void OnFormatParameterName(object sender, FormatParameterEventArgs e) => FormatParameterName?.Invoke(sender, e);

		/// <summary>
		/// Gets or sets a data connection which will be used to create commands and command parameters.
		/// </summary>
		public TConnection DataConnection { get; set; }

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
		/// Gets the conditions which are required in order to build a valid query. There can be one condition for each query parameter.
		/// </summary>
		public List<Condition> Conditions { get; internal set; } = new List<Condition>();

		/// <summary>
		/// Gets or sets a collection of variables that will be passed onto the parser engine.
		/// </summary>
		public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Gets a list of errors that were generated by the parser engine after calling CreateCommand method.
		/// </summary>
		public string ParserErrors { get; private set; }

		/// <summary>
		/// Gets a list of warnings that were generated by the parser engine after calling CreateCommand method.
		/// </summary>
		public string ParserWarnings { get; private set; }

		/// <summary>
		/// Gets a <see cref="TCommand"/> associated with this query.
		/// </summary>
		public TCommand DbCommand { get; private set; } 
		
		/// <summary>
		/// Creates a condition for the query.
		/// </summary>
		/// <param name="parameterName">Name of the parameter for which this condition applies.</param>
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
		/// <param name="parameterName">Name of the query parameter for which this condition applies.</param>
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
		/// <param name="name">The name of the variable.</param>
		/// <param name="value">The value.</param>
		public virtual void DefineVariable(string name, object value) => Variables[name] = value;

		private readonly HashSet<Condition> _processedConditions = new HashSet<Condition>();

		/// <summary>
		/// Processes the script and creates a <see cref="TCommand"/> command.
		/// </summary>			
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public virtual TCommand CreateCommand()
		{
			var parser = new Parser();

			parser.RequestParameterValue += Parser_RequestParameterValue;

			DbCommand = (TCommand)DataConnection.CreateCommand();
			DbCommand.CommandType = CommandType.Text;

			ParserBuffer pr;

			try
			{
				pr = parser.Parse(SqlBinderScript);
			}
			catch (Exception ex)
			{
				throw new ParserException(ex);
			}

			if (!pr.IsValid)
			{
				ParserErrors = pr.Errors;

				if (ThrowScriptErrorException)
				{
					if (pr.CompileException != null)
						throw new ParserException(pr.CompileException);					
					throw new ParserException(pr.Errors);
				}

				if (LogScriptErrorException)
				{
					if (pr.CompileException != null)
						System.Diagnostics.Debug.WriteLine(new ParserException(pr.CompileException).Message);
					else
						System.Diagnostics.Debug.WriteLine(new ParserException(pr.Errors).Message);
				}
			}

			var unprocessedConditions = Conditions.Except(_processedConditions).ToArray();
			if (unprocessedConditions.Any())
				throw new UnmatchedConditionsException(unprocessedConditions);

			ParserWarnings = pr.Warnings;

			DbCommand.CommandText = pr.Output;

			return DbCommand;
		}		

		private void Parser_RequestParameterValue(object sender, RequestParameterArgs e)
		{
			var cond = Conditions.FirstOrDefault(c => string.CompareOrdinal(c.Parameter, e.Parameter.Name) == 0);
			
			if (cond != null)
			{
				_processedConditions.Add(cond);

				if (e.Parameter.IsCompound)
					e.Values = cond.Value.GetValues().Select(v => (object)$"'{v}'").ToArray();
				else
				{
					var sql = ConstructParameterSql(cond.Operator, cond.Value, cond.Parameter);
					e.Values = new object[] { sql };
				}

				e.Processed = true;
			}
			else
			{
				var variableName = e.Parameter.Name + e.Parameter.Member;
				object variableValue = null;

				if (Variables.ContainsKey(variableName))
					variableValue = Variables[variableName];

				if (variableValue != null)
				{
					if (variableValue.GetType().IsArray)
						e.Values = (object[])variableValue;
					else
						e.Values = new[] { variableValue };

					e.Processed = true;
				}
			}
		}

		/// <summary>
		/// Compiles a command parameter sql based on query parameter, operator and value.
		/// </summary>
		protected virtual string ConstructParameterSql(Operator sqlOperator, ConditionValue conditionValue, string parameter)
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

				// Create command paramete(s) for each value
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
								var paramName = $"p{parameter}_{paramCnt}";
								var param = AddCommandParameter(paramName, subValue);
								conditionValue.ProcessParameter(param);
								sqlParamNames.Add(FormatParameterNameInternal(paramName));
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
							var paramName = $"p{parameter}_{paramCnt}";
							var param = AddCommandParameter(paramName, value);
							conditionValue.ProcessParameter(param);
							paramsSql[i] = FormatParameterNameInternal(paramName);
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

		/// <summary>
		/// Delegate that can be used to intercept and alter command parameters on the fly. Use this to pass custom DBMS parameters. This is called
		/// before the parameter is added to command so you can either return the same reference or create your own.
		/// </summary>
		public Func<IDbDataParameter, IDbDataParameter> PrepareCmdParameter = p => p;

		/// <summary>
		/// Adds a parameter to the output command, parameter type will be resolved by the virtual OnResolveDbType method.
		/// </summary>
		protected virtual IDbDataParameter AddCommandParameter(string paramName, object paramValue)
		{
			var param = DbCommand.CreateParameter();

			param.Direction = ParameterDirection.Input;

			if (paramValue == null)
				param.DbType = DbType.Object;
			else
			{
				param.DbType = OnResolveDbType(paramValue.GetType());

				if (paramValue is char)
					param.Size = 1;
			}

			param.Value = paramValue ?? DBNull.Value;
			param.ParameterName = paramName;

			param = PrepareCmdParameter(param);

			DbCommand.Parameters.Add(param);

			return param;
		}
	}
}
