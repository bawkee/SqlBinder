namespace SqlBinder.MultiBinder
{
	/// <summary>
	/// Represents a parameter binding placeholder in the template script. These parameters are enclosed by '[' and ']' brackets. These parameters will be
	/// replaced by command parameters once it is parsed and command is created.
	/// </summary>
	public class TemplateParameter
	{
		internal TemplateParameter() { }

		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether this parameter will be used by the <see cref="Query"/>.
		/// </summary>
		public bool IsUsed { get; internal set; }

		/// <summary>
		/// Gets template flags if there are any. Flags are separated by the pipe character
		/// in the template script (eg. [parameter | flag1 | flag2]).
		/// </summary>
		public string[] Flags { get; internal set; }
	}
}
