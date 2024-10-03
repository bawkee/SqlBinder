using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly object[] _values = { };

        public StringValue(string value, MatchOption matchOption = MatchOption.ExactMatch, string wildCard = "%")
        {
            if (value != null)
                _values = new object[] { TranslateValue(value, matchOption, wildCard) };
        }

        public StringValue(string from, string to)
            => _values = new object[] { from ?? string.Empty, to ?? string.Empty };

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

        public StringValue(IEnumerable<string> values)
        {
            var arr = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
            if (arr.All(v => v == null))
                throw new ArgumentNullException(nameof(values));
            _values = new[] { ReduceEnum(arr) ?? values };
        }

        protected override object[] OnGetValues() => _values;

        private bool IsValueList() => _values.Any() && IsList(_values[0]);

        protected override string OnGetSql(int sqlOperator)
        {
            switch (sqlOperator)
            {
                case (int)Operator.Is:
                    return _values.Length == 0 ? "IS NULL" : ValidateParams("= {0}", 1);
                case (int)Operator.IsNot:
                    return _values.Length == 0 ? "IS NOT NULL" : ValidateParams("<> {0}", 1);
                case (int)Operator.IsBetween: return ValidateParams("BETWEEN {0} AND {1}", 2);
                case (int)Operator.IsNotBetween: return ValidateParams("NOT BETWEEN {0} AND {1}", 2);
                case (int)Operator.IsAnyOf:
                    if (!IsValueList())
                        return ValidateParams("= {0}", 1);
                    return ValidateParams("IN ({0})", 1, true);
                case (int)Operator.IsNotAnyOf:
                    if (!IsValueList())
                        return ValidateParams("!= {0}", 1);
                    return ValidateParams("NOT IN ({0})", 1, true);
                case (int)Operator.Contains: return ValidateParams("LIKE {0}", 1);
                case (int)Operator.DoesNotContain: return ValidateParams("NOT LIKE {0}", 1);
                default:
                    throw new InvalidConditionException(this, (Operator)sqlOperator,
                        Exceptions.IllegalComboOfValueAndOperator);
            }
        }
    }
}