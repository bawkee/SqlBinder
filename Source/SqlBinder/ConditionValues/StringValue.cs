using System.Collections.Generic;
using SqlBinder.Properties;

namespace SqlBinder.ConditionValues
{
	/// <summary>
	/// Allows passing string values into a <see cref="Condition"/>.
	/// </summary>
	public class StringValue : ConditionValue
	{
		/// <summary>
		/// Options for building a 'LIKE' SQL expression value
		/// </summary>
		public enum MatchOption
		{
			ExactMatch,
			OccursAnywhere,
			BeginsWith,
			EndsWith
		}

		private readonly object[] _values;

		public StringValue(string value, MatchOption matchOption = MatchOption.ExactMatch, string wildCard = "%")
			=> _values = new object[] {TranslateValue(value, matchOption, wildCard) };

		public StringValue(string from, string to)
			=> _values = new object[] {from, to};

		private static string TranslateValue(string value, MatchOption matchOption, string wildCard)
		{
			switch (matchOption)
			{
				case MatchOption.BeginsWith: return $"{value}{wildCard}";
				case MatchOption.EndsWith: return $"{wildCard}{value}";
				case MatchOption.OccursAnywhere: return $"{wildCard}{value}{wildCard}";
				default: return value;
			}
		}
		
		public StringValue(IEnumerable<string> values) => _values = new object[] { values };

		protected override object[] OnGetValues() => _values;

		protected override string OnGetSql(int sqlOperator)
		{
			switch (sqlOperator)
			{
				case (int)Operator.Is: return "= {0}";
				case (int)Operator.IsNot: return "!= {0}";
				case (int)Operator.IsBetween: return "BETWEEN {0} AND {1}";
				case (int)Operator.IsNotBetween: return "NOT BETWEEN {0} AND {1}";
				case (int)Operator.IsAnyOf: return "IN ({0})";
				case (int)Operator.IsNotAnyOf: return "NOT IN ({0})";
				case (int)Operator.Contains: return "LIKE {0}";
				case (int)Operator.DoesNotContain: return "NOT LIKE {0}";
				default: throw new InvalidConditionException(this, (Operator) sqlOperator, Exceptions.IllegalComboOfValueAndOperator);
			}
		}
	}
}
