using System.Data.OleDb;

namespace SqlBinder.ConsoleTutorial
{
	public class OleDbQuery : QueryBase<OleDbConnection, OleDbCommand>
	{
		protected override string DefaultParameterFormat => "@{0}";

		public OleDbQuery(OleDbConnection connection)
			: base(connection)
		{
		}

		public OleDbQuery(OleDbConnection connection, string script)
			: base(connection, script)
		{
		}
	}
}
