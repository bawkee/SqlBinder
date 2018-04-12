namespace SqlBinder.MultiBinder
{
	/// <summary>
	/// Represents a field that will be used in <see cref="Query"/> execution. Query parameters will be matched against <see cref="TemplateParameter"/>s. This class
	/// is used only when creating queries based on multiple templates (template classes).
	/// </summary>
	public class QueryParameter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QueryParameter"/> class.
		/// </summary>
		/// <param name="name">Name of the parameter. This should match a <see cref="TemplateParameter"/> name.</param>
		public QueryParameter(string name)
		{
			Name = name;
			DefaultOperator = SqlOperator.Is;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryParameter"/> class.
		/// </summary>
		/// <param name="name">Name of the parameter. This should match a <see cref="TemplateParameter"/> name.</param>
		/// <param name="defaultOperator">The default operator that will be passed onto a future potential condition.</param>
		public QueryParameter(string name, SqlOperator defaultOperator)
		{
			Name = name;
			DefaultOperator = defaultOperator;
		}

		/// <summary>
		/// Gets or sets the name of the parameter. This should match a <see cref="TemplateParameter"/> name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the default operator that will be passed onto a future potential <see cref="Condition"/>.
		/// </summary>
		public SqlOperator DefaultOperator { get; set; }
	}
}
