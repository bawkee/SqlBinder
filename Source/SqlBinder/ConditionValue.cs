using System.Data;

namespace SqlBinder
{
	/// <summary>
	/// Abstract class that provides infrastructure to implement custom condition values.
	/// </summary>
	public abstract class ConditionValue
	{
		internal object[] GetValues() => OnGetValues();
		internal string GetSql(int sqlOperator) => OnGetSql(sqlOperator);
		internal void ProcessParameter(IDbDataParameter parameter) => OnProcessParameter(parameter);

		/// <summary>
		/// Allows custom processing logic for any parameter that resulted from this <see cref="ConditionValue"/>.
		/// </summary>
		protected virtual void OnProcessParameter(IDbDataParameter parameter) { }

		/// <summary>
		/// When overriden in a derived class it must return an array of values that match the format string returned by <see cref="OnGetSql(int)"/>
		/// </summary>
		protected abstract object[] OnGetValues();

		/// <summary>
		/// When overriden in a derived class it must return a format specifier for the values returned in <see cref="OnGetValues"/>
		/// </summary>
		/// <param name="sqlOperator"></param>
		protected abstract string OnGetSql(int sqlOperator);
	}
}
