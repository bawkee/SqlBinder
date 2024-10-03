using System;
using System.Collections.Generic;

namespace SqlBinder.ConditionValues
{
    /// <summary>
    /// Allows passing date/time values into a <see cref="Condition"/>.
    /// </summary>
    public class DateValue : NumberValue
    {
        public DateValue(DateTime? dateValue) => SetValue(dateValue);

        public DateValue(DateTime from, DateTime to) => SetValues(from, to);

        public DateValue(IEnumerable<DateTime> dateValues) => SetValue(dateValues);
    }
}