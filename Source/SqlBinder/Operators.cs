using System;

namespace SqlBinder
{
	/// <summary>
	/// Basic sql operators that can be used with conditions. Please note that if you want to use custom operators on custom conditions, you should start
	/// with 50 (0x32), as everything below is reserved and may be used internally.
	/// </summary>
	public enum Operator
	{
		/// <summary>
		/// Not defined (condition without operator)
		/// </summary>
		None,
		/// <summary>
		/// Equals, '= :p1'
		/// </summary>
		Is,
		/// <summary>
		/// Does not equal, '!= :p1'
		/// </summary>
		IsNot,
		/// <summary>
		/// Is in, 'IN (:p1, :p2)'
		/// </summary>
		IsAnyOf,
		/// <summary>
		/// Is not in, 'NOT IN (:p1, :p2)'
		/// </summary>
		IsNotAnyOf,
		/// <summary>
		/// Is between, 'BETWEEN :p1 AND :p2'
		/// </summary>
		IsBetween,
		/// <summary>
		/// Is not between, 'NOT BETWEEN :p1 AND :p2'
		/// </summary>
		IsNotBetween,
		/// <summary>
		/// Contains a string value, 'LIKE :p1'
		/// </summary>
		Contains,
		/// <summary>
		/// Does not contain a string value, 'NOT LIKE :p1'
		/// </summary>
		DoesNotContain,
		/// <summary>
		/// Less than, '&lt; :p1'
		/// </summary>
		IsLessThan,
		/// <summary>
		/// Less than or equal to, '&lt;= :p1'
		/// </summary>
		IsLessThanOrEqualTo,
		/// <summary>
		/// Greater than, '&gt; :p1'
		/// </summary>
		IsGreaterThan,
		/// <summary>
		/// Greater than or equal to, '&gt;= :p1'
		/// </summary>
		IsGreaterThanOrEqualTo
	}

	/// <summary>
	/// Basic operators for the condition shortcut overloads.
	/// </summary>
	public enum NumericOperator
	{
		Is,
		IsNot,
		IsLessThan,
		IsLessThanOrEqualTo,
		IsGreaterThan,
		IsGreaterThanOrEqualTo
	}

	/// <summary>
	/// Basic operators for string condition shortcut overloads.
	/// </summary>
	public enum StringOperator
	{
		Is,
		IsNot,
		IsLike,
		IsNotLike
	}
}
