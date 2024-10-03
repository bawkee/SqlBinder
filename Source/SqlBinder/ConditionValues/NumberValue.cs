using System;
using System.Collections.Generic;
using System.Linq;
using SqlBinder.Properties;

namespace SqlBinder.ConditionValues
{
    /// <summary>
    /// Allows passing numeric values into a <see cref="Condition"/>.
    /// </summary>
    public class NumberValue : ConditionValue
    {
        private object[] _values = [];

        protected NumberValue()
        {
        }

        public NumberValue(decimal value) => SetValue(value);
        public NumberValue(double value) => SetValue(value);
        public NumberValue(float value) => SetValue(value);
        public NumberValue(int value) => SetValue(value);
        public NumberValue(uint value) => SetValue(value);
        public NumberValue(long value) => SetValue(value);
        public NumberValue(ulong value) => SetValue(value);
        public NumberValue(byte value) => SetValue(value);
        public NumberValue(sbyte value) => SetValue(value);
        public NumberValue(short value) => SetValue(value);
        public NumberValue(ushort value) => SetValue(value);
        public NumberValue(char value) => SetValue(value);

        public NumberValue(decimal? value) => SetValue(value);
        public NumberValue(double? value) => SetValue(value);
        public NumberValue(float? value) => SetValue(value);
        public NumberValue(int? value) => SetValue(value);
        public NumberValue(uint? value) => SetValue(value);
        public NumberValue(long? value) => SetValue(value);
        public NumberValue(ulong? value) => SetValue(value);
        public NumberValue(byte? value) => SetValue(value);
        public NumberValue(sbyte? value) => SetValue(value);
        public NumberValue(short? value) => SetValue(value);
        public NumberValue(ushort? value) => SetValue(value);
        public NumberValue(char? value) => SetValue(value);

        public NumberValue(decimal from, decimal to) => SetValues(from, to);
        public NumberValue(double from, double to) => SetValues(from, to);
        public NumberValue(float from, float to) => SetValues(from, to);
        public NumberValue(int from, int to) => SetValues(from, to);
        public NumberValue(uint from, uint to) => SetValues(from, to);
        public NumberValue(long from, long to) => SetValues(from, to);
        public NumberValue(ulong from, ulong to) => SetValues(from, to);
        public NumberValue(byte from, byte to) => SetValues(from, to);
        public NumberValue(sbyte from, sbyte to) => SetValues(from, to);
        public NumberValue(short from, short to) => SetValues(from, to);
        public NumberValue(ushort from, ushort to) => SetValues(from, to);
        public NumberValue(char from, char to) => SetValues(from, to);

        public NumberValue(IEnumerable<decimal> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<double> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<float> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<int> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<uint> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<long> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<ulong> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<byte> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<sbyte> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<short> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<ushort> values) => SetValues(ReduceEnum(values) ?? values);
        public NumberValue(IEnumerable<char> values) => SetValues(ReduceEnum(values) ?? values);

        protected void SetValue(object value) => _values = value == null ? [] : [value];

        protected void SetValues(params object[] values)
        {
            if (values?.All(v => v == null) ?? true)
                throw new ArgumentNullException(nameof(values)); // All nulls?
            if (values.Length == 2)
            {
                if (values[0] == null || values[1] == null)
                    throw new ArgumentException(Exceptions.NullIsMutuallyExclusiveWIthEverythingElse, nameof(values));
                if (values[0].Equals(values[1]))
                    values = [values[0]];
            }

            _values = values;
        }

        protected override object[] OnGetValues() => _values;

        private bool IsValueList() => _values.Any() && IsList(_values[0]);

        protected override string OnGetSql(int sqlOperator) =>
            sqlOperator switch
            {
                (int)Operator.Is => _values.Length == 0 ? "IS NULL" : ValidateParams("= {0}", 1),
                (int)Operator.IsNot => _values.Length == 0 ? "IS NOT NULL" : ValidateParams("<> {0}", 1),
                (int)Operator.IsLessThan => ValidateParams("< {0}", 1),
                (int)Operator.IsLessThanOrEqualTo => ValidateParams("<= {0}", 1),
                (int)Operator.IsGreaterThan => ValidateParams("> {0}", 1),
                (int)Operator.IsGreaterThanOrEqualTo => ValidateParams(">= {0}", 1),
                (int)Operator.IsBetween => _values.Length switch
                {
                    2 => ValidateParams("BETWEEN {0} AND {1}", 2),
                    1 => ValidateParams("= {0}", 1),
                    _ => throw new InvalidOperationException(Exceptions.PlaceholdersAndActualParamsDontMatch)
                },
                (int)Operator.IsNotBetween => _values.Length switch
                {
                    2 => ValidateParams("NOT BETWEEN {0} AND {1}", 2),
                    1 => ValidateParams("<> {0}", 1),
                    _ => throw new InvalidOperationException(Exceptions.PlaceholdersAndActualParamsDontMatch)
                },
                (int)Operator.IsAnyOf => !IsValueList()
                    ? ValidateParams("= {0}", 1)
                    : ValidateParams("IN ({0})", 1, true),
                (int)Operator.IsNotAnyOf => !IsValueList()
                    ? ValidateParams("<> {0}", 1)
                    : ValidateParams("NOT IN ({0})", 1, true),
                _ => throw new InvalidConditionException(this, (Operator)sqlOperator,
                    Exceptions.IllegalComboOfValueAndOperator)
            };
    }
}