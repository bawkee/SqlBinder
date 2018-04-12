using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.DemoNorthwindDal.OleDb
{
	public class OleDbSqlBinder : SqlBinder
	{
		public OleDbSqlBinder(OleDbConnection dataConnection)
			: base(dataConnection) { }

		protected override string DefaultParameterFormat => "@{0}";
	}

}
