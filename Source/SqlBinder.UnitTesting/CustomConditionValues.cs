using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlBinder.Properties;

namespace SqlBinder.UnitTesting
{
    public class CustomParameterlessConditionValue : ConditionValue
    {
        private readonly string _value;

        public override bool UseBindVariables => false;

        public CustomParameterlessConditionValue(string value)
        {
            _value = value;
        }

        protected override string OnGetSql(int sqlOperator)
        {
            switch ((Operator)sqlOperator)
            {
                case Operator.Is: return $"= '{_value}' /*hint*/";
                case Operator.IsNot: return $"<> '{_value}'";
            }

            throw new InvalidConditionException(this, (Operator)sqlOperator, Exceptions.IllegalComboOfValueAndOperator);
        }
    }

    public class CustomConditionValue : ConditionValue
    {
        private readonly object[] _values;

        public CustomConditionValue(int value1, int value2, int value3) =>
            _values = new object[] { value1, value2, value3 };

        protected override object[] OnGetValues() => _values;

        protected override string OnGetSql(int sqlOperator)
        {
            switch ((Operator)sqlOperator)
            {
                case Operator.Is: return "= sillyProcedure({0}, {1}, {2})";
            }

            throw new InvalidConditionException(this, (Operator)sqlOperator, Exceptions.IllegalComboOfValueAndOperator);
        }
    }
}