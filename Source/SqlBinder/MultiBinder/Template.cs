namespace SqlBinder.MultiBinder
{
	/// <summary>
	/// Represents a template script.
	/// </summary>
	public class Template
	{
		internal Template() { }

		//TODO: No associated binder?

		/// <summary>
		/// Gets the class of the template. Templates are grouped into classes to allow use of multiple templates 
		/// and chosing the best one.
		/// </summary>
		public string Class { get; internal set; }

		/// <summary>
		/// Gets the name identifier of the template.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Gets the actual template script / contents.
		/// </summary>
		public string Script { get; internal set; }
	}
}
