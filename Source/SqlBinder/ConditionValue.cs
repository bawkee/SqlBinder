using System;
using System.Collections;
using System.Data;
using System.Linq;
using SqlBinder.Properties;

namespace SqlBinder
{
	/// <summary>
	/// Abstract class that provides infrastructure to implement custom condition values.
	/// </summary>
	public abstract class ConditionValue
	{
		internal object[] GetValues() => OnGetValues();
		internal string GetSql(int sqlOperator) => OnGetSql(sqlOperator);
		internal string GetSql(Operator sqlOperator) => OnGetSql((int)sqlOperator);
		internal void ProcessParameter(IDbDataParameter parameter) => OnProcessParameter(parameter);

		/// <summary>
		/// Gets or sets a value indicating whether <see cref="QueryBase{TCONNECTION,TCOMMAND}"/> should output an SQL with a bind variable (command parameter) or just
		/// plain SQL.
		/// </summary>
		public virtual bool UseBindVariables { get; } = true;

		/// <summary>
		/// Allows custom processing logic for any parameter that resulted from this <see cref="ConditionValue"/>.
		/// </summary>
		protected virtual void OnProcessParameter(IDbDataParameter parameter) { }

		/// <summary>
		/// When overriden in a derived class it must return an array of values that match the format string returned by <see cref="OnGetSql(int)"/>.
		/// </summary>
		protected virtual object[] OnGetValues() => null;

		/// <summary>
		/// When overriden in a derived class it must return a format specifier for the values returned in <see cref="OnGetValues"/>. Operator type is
		/// int to allow for custom operators. 
		/// </summary>
		protected abstract string OnGetSql(int sqlOperator);

		protected bool IsList(object var) => !(var is string) && var is IEnumerable;

		/// <summary>
		/// Use this to validate sql string format against value count.
		/// </summary>
		protected virtual string ValidateParams(string sql, int paramCount, bool allowLists = false)
		{
			if (GetValues().Length != paramCount || !allowLists && GetValues().Any(IsList))
				throw new InvalidOperationException(Exceptions.PlaceholdersAndActualParamsDontMatch);
			return sql;
		}

		/// <summary>
		/// If enumerable has just one element then its purpose is pointess and help reduce it to optimize the whole process and potentially SQL.
		/// </summary>
		protected object ReduceEnum(IEnumerable input)
		{
			if (input == null)
				return null;
			var e = input.GetEnumerator();
			if (!e.MoveNext())
				return null;
			var item = e.Current;
			if (!e.MoveNext())
				return item;
			return null;
		}
	}
}
