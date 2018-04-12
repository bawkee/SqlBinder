using System;
using System.Collections.Generic;

namespace SqlBinder.ConditionValues
{
	/// <summary>
	/// Allows passing date/time values into a <see cref="Condition"/>.
	/// </summary>
	public class DateValue : NumberValue
	{
		private readonly object[] _dates;

		public DateValue(DateTime? dateValue) => _dates = new object[] { dateValue };

		public DateValue(DateTime from, DateTime to) => _dates = new object[] {from, to};

		public DateValue(IEnumerable<DateTime> dateValues) => _dates = new object[] { dateValues };

		protected override object[] OnGetValues() => _dates;
	}
}
