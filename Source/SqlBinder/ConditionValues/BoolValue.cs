using SqlBinder.Properties;

namespace SqlBinder.ConditionValues
{
    /// <summary>
    /// Allows passing a boolean value into a <see cref="Condition"/>.
    /// </summary>
    public class BoolValue : ConditionValue
    {
        private readonly object _value;

        public BoolValue(bool? value)
        {
            _value = value;
        }

        protected override object[] OnGetValues()
        {
            return _value == null ? new object[] { } : new[] { _value };
        }

        protected override string OnGetSql(int sqlOperator)
        {
            switch (sqlOperator)
            {
                case (int)Operator.Is:
                    return _value == null ? "IS NULL" : "= {0}";
                case (int)Operator.IsNot:
                    return _value == null ? "IS NOT NULL" : "<> {0}";
                default:
                    throw new InvalidConditionException(this, (Operator)sqlOperator,
                        Exceptions.IllegalComboOfValueAndOperator);
            }
        }
    }
}