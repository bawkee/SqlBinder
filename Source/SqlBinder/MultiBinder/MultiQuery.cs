using System.Linq;
using SqlBinder.ConditionValues;
using SqlBinder.Properties;

namespace SqlBinder.MultiBinder
{
	/// <summary>
	/// A query that resulted from a MultiSqlBinder operation as a best match among multiple templates.
	/// </summary>
	public class MultiQuery : Query
	{
		internal MultiQuery(SqlBinder sq) : base(sq)
		{
			//
		}

		/// <summary>
		/// Gets the SqlBinder template script that was chosen for this query.
		/// </summary>
		public Template Template { get; internal set; }

		/// <summary>
		/// Gets templates that belong to the same class as the main template. These templates may be referenced and used by the main template.
		/// </summary>
		public Template[] ClassTemplates { get; internal set; }

		/// <summary>
		/// Gets the collection of parameters that were chosen during query creation.
		/// </summary>
		public QueryParameter[] QueryParameters { get; internal set; }

		/// <summary>
		/// Gets the template parameters that were found in the template script.
		/// </summary>
		public TemplateParameter[] TemplateParameters { get; internal set; }

		/// <summary>
		/// Finds a parameter by its name. Useful when creating conditions.
		/// </summary>
		public QueryParameter GetQueryParameter(string name) => QueryParameters.FirstOrDefault(p => string.CompareOrdinal(p.Name, name) == 0);

		/// <inheritdoc />
		public override void SetCondition(string parameterName, SqlOperator op, ConditionValue value)
		{
			var param = GetQueryParameter(parameterName);

			if (param != null)
			{
				RemoveCondition(parameterName);
				Conditions.Add(new Condition(param.Name, op, value));
			}
			else
				throw new InvalidConditionException(value, op, string.Format(Exceptions.ParamDoesNotExist, parameterName));
		}

		/// <summary>
		/// Creates a <see cref="Condition"/> for the query with the default operator for given parameter.
		/// </summary>
		/// <param name="parameterName">Name of the <see cref="QueryParameter"/> for which this condition applies.</param>
		/// <param name="value">Value of the condition. You can use your own or already predefined classes
		/// such as <see cref="DateValue"/>, <see cref="NumberValue"/>, <see cref="StringValue"/> or <see cref="BoolValue"/>.</param>
		public override void SetCondition(string parameterName, ConditionValue value)
		{
			SetCondition(parameterName, GetQueryParameter(parameterName)?.DefaultOperator ?? SqlOperator.None, value);
		}
	}
}
