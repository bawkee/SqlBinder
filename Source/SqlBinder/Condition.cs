using SqlBinder.ConditionValues;

namespace SqlBinder
{
	/// <summary>
	/// Allows to associate a script parameter with a condition. If a script parameter does not have a condition it will be ignored.
	/// </summary>
	public class Condition
	{
		internal Condition(string parameter, Operator op, ConditionValue val)
		{
			Parameter = parameter;
			Value = val;
			Operator = op;
		}

		/// <summary>
		/// Gets the parameter associated with this condition..
		/// </summary>
		public string Parameter { get; internal set; }

		/// <summary>
		/// Gets or sets the operator for this condition.
		/// </summary>
		public Operator Operator { get; set; }

		/// <summary>
		/// Gets or sets the value of the condition. You can use your own or already predefined classes
		/// such as <see cref="DateValue"/>, <see cref="NumberValue"/>, <see cref="StringValue"/> or <see cref="BoolValue"/>.
		/// </summary>
		public ConditionValue Value { get; set; }
	}
}
