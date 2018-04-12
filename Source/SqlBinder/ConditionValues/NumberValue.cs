using System.Collections.Generic;
using SqlBinder.Properties;

namespace SqlBinder.ConditionValues
{
	/// <summary>
	/// Allows passing numeric values into a <see cref="Condition"/>.
	/// </summary>
	public class NumberValue : ConditionValue
	{
		private readonly object[] _values;

		protected NumberValue() { }

		public NumberValue(decimal value) => _values = new object[] { value };
		public NumberValue(double value) => _values = new object[] { value };
		public NumberValue(float value) => _values = new object[] { value };
		public NumberValue(int value) => _values = new object[] { value };
		public NumberValue(uint value) => _values = new object[] { value };
		public NumberValue(long value) => _values = new object[] { value };
		public NumberValue(ulong value) => _values = new object[] { value };
		public NumberValue(byte value) => _values = new object[] { value };
		public NumberValue(sbyte value) => _values = new object[] { value };
		public NumberValue(short value) => _values = new object[] { value };
		public NumberValue(ushort value) => _values = new object[] { value };
		public NumberValue(char value) => _values = new object[] { value };

		public NumberValue(decimal? value) => _values = new object[] { value };
		public NumberValue(double? value) => _values = new object[] { value };
		public NumberValue(float? value) => _values = new object[] { value };
		public NumberValue(int? value) => _values = new object[] { value };
		public NumberValue(uint? value) => _values = new object[] { value };
		public NumberValue(long? value) => _values = new object[] { value };
		public NumberValue(ulong? value) => _values = new object[] { value };
		public NumberValue(byte? value) => _values = new object[] { value };
		public NumberValue(sbyte? value) => _values = new object[] { value };
		public NumberValue(short? value) => _values = new object[] { value };
		public NumberValue(ushort? value) => _values = new object[] { value };
		public NumberValue(char? value) => _values = new object[] { value };

		public NumberValue(decimal from, decimal to) => _values = new object[] {from, to};
		public NumberValue(double from, double to) => _values = new object[] { from, to };
		public NumberValue(float from, float to) => _values = new object[] { from, to };
		public NumberValue(int from, int to) => _values = new object[] { from, to };
		public NumberValue(uint from, uint to) => _values = new object[] { from, to };
		public NumberValue(long from, long to) => _values = new object[] { from, to };
		public NumberValue(ulong from, ulong to) => _values = new object[] { from, to };
		public NumberValue(byte from, byte to) => _values = new object[] { from, to };
		public NumberValue(sbyte from, sbyte to) => _values = new object[] { from, to };
		public NumberValue(short from, short to) => _values = new object[] { from, to };
		public NumberValue(ushort from, ushort to) => _values = new object[] { from, to };
		public NumberValue(char from, char to) => _values = new object[] { from, to };

		public NumberValue(IEnumerable<decimal> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<double> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<float> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<int> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<uint> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<long> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<ulong> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<byte> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<sbyte> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<short> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<ushort> values) => _values = new object[] { values };
		public NumberValue(IEnumerable<char> values) => _values = new object[] { values };

		protected override object[] OnGetValues() => _values;

		protected override string OnGetSql(int sqlOperator)
		{			
			switch (sqlOperator)
			{
				case (int)Operator.Is: return "= {0}";
				case (int)Operator.IsNot: return "!= {0}";
				case (int)Operator.IsLessThan: return "< {0}";
				case (int)Operator.IsLessThanOrEqualTo: return "<= {0}";
				case (int)Operator.IsGreaterThan: return "> {0}";
				case (int)Operator.IsGreaterThanOrEqualTo: return ">= {0}";
				case (int)Operator.IsBetween: return "BETWEEN {0} AND {1}";
				case (int)Operator.IsNotBetween: return "NOT BETWEEN {0} AND {1}";
				case (int)Operator.IsAnyOf: return "IN ({0})";
				case (int)Operator.IsNotAnyOf: return "NOT IN ({0})";
				default: throw new InvalidConditionException(this, (Operator)sqlOperator, Exceptions.IllegalComboOfValueAndOperator);
			}
		}
	}
}
