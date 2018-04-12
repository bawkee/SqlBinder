using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SqlBinder.Properties;

namespace SqlBinder
{
	public class ParserException : Exception
	{
		public ParserException(Exception innerException) : base(Exceptions.ParserFailure, innerException) { }
	}

	public class UnmatchedConditionsException : Exception
	{
		public UnmatchedConditionsException(IEnumerable<Condition> conditions) : base(string.Format(Exceptions.NoMatchingParams,
			string.Join(", ", conditions.Select(c => c.Parameter).ToArray()))) { }
	}

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
	/// Creates queries and defines global variables. It can also serve as abstraction for custom implementations of different DBMS types.
	/// </summary>
	public class SqlBinder
	{
		/// <summary>
		/// Gets or sets a data connection associated with this SqlBinder.
		/// </summary>
		public IDbConnection DataConnection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether script parser exceptions should be thrown.
		/// </summary>
		public bool ThrowScriptErrorException { get; set; }

		/// <summary>
		/// Gets or sets a collection of variables that will be passed onto the parser engine.
		/// </summary>
		public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Occurs when parameter is to be formatted for the SQL ouput. Use this to specify custom parameter tags for your DBMS.
		/// </summary>
		public event FormatParameterEventHandler FormatParameterName;
		protected virtual void OnFormatParameterName(object sender, FormatParameterEventArgs e) => FormatParameterName?.Invoke(sender, e);

		/// <summary>
		/// Gets or sets default parameter format string for all queries. See <see cref="FormatParameterName"/> event.
		/// </summary>
		protected virtual string DefaultParameterFormat { get; } = "{0}";

		/// <summary>
		/// Basic, assumed, ADO type mappings. This can be overriden on any level (SqlBinder, Query, ConditionValue).
		/// </summary>
		private static Dictionary<Type, DbType> _dbTypeMap { get; } =
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

		internal DbType ResolveDbType(Type clrType) => OnResolveDbType(clrType);

		/// <summary>
		/// When overriden in a derived class it allows customizing ADO DbType guessing based on clr types.
		/// </summary>
		protected virtual DbType OnResolveDbType(Type clrType)
		{
			var clrTypeNN = Nullable.GetUnderlyingType(clrType) ?? clrType;
			return _dbTypeMap.ContainsKey(clrTypeNN) ? _dbTypeMap[clrTypeNN] : DbType.Object;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlBinder"/> class.
		/// </summary>
		/// <param name="dataConnection">Active data connection.</param>
		public SqlBinder(IDbConnection dataConnection) => DataConnection = dataConnection;

		/// <summary>
		/// Defines a user variable that can be used when a <see cref="Query"/> is executed by
		/// the template parser engine.
		/// </summary>
		/// <param name="name">The name of the variable.</param>
		/// <param name="value">The value.</param>
		public virtual void DefineVariable(string name, object value) => Variables[name] = value;		

		/// <summary>
		/// Creates a <see cref="Query"/> based on provided SqlBinder script.
		/// </summary>
		public virtual Query CreateQuery(string query) => new Query(this, query);

		/// <summary>
		/// Fires an event which can be used to format all or some queries and can be overriden in a derived class.
		/// </summary>
		internal string FormatParameterNameInternal(Query sender, string parameterName)
		{
			var e = new FormatParameterEventArgs
			{
				ParameterName = parameterName,
				FormattedName = string.Format(DefaultParameterFormat, parameterName)
			};
			OnFormatParameterName(sender, e);
			return e.FormattedName;
		}
	}
}
