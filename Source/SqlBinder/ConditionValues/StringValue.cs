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

        private readonly object[] _values = [];

        public StringValue(string value, MatchOption matchOption = MatchOption.ExactMatch, string wildCard = "%")
        {
            if (value != null)
                _values = [TranslateValue(value, matchOption, wildCard)];
        }

        public StringValue(string from, string to)
            => _values = [from ?? string.Empty, to ?? string.Empty];

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
            _values = [ReduceEnum(arr) ?? values];
        }

        protected override object[] OnGetValues() => _values;

        private bool IsValueList() => _values.Any() && IsList(_values[0]);

        protected override string OnGetSql(int sqlOperator)
        {
            return sqlOperator switch
            {
                (int)Operator.Is => _values.Length == 0 ? "IS NULL" : ValidateParams("= {0}", 1),
                (int)Operator.IsNot => _values.Length == 0 ? "IS NOT NULL" : ValidateParams("<> {0}", 1),
                (int)Operator.IsBetween => ValidateParams("BETWEEN {0} AND {1}", 2),
                (int)Operator.IsNotBetween => ValidateParams("NOT BETWEEN {0} AND {1}", 2),
                (int)Operator.IsAnyOf => !IsValueList()
                    ? ValidateParams("= {0}", 1)
                    : ValidateParams("IN ({0})", 1, true),
                (int)Operator.IsNotAnyOf => !IsValueList()
                    ? ValidateParams("!= {0}", 1)
                    : ValidateParams("NOT IN ({0})", 1, true),
                (int)Operator.Contains => ValidateParams("LIKE {0}", 1),
                (int)Operator.DoesNotContain => ValidateParams("NOT LIKE {0}", 1),
                _ => throw new InvalidConditionException(this, (Operator)sqlOperator,
                    Exceptions.IllegalComboOfValueAndOperator)
            };
        }
    }
}