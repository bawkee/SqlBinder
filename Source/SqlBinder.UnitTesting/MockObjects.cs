using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace SqlBinder.UnitTesting
{
	public class MockQuery : DbQuery
	{
		public MockQuery(MockDbConnection connection) 
			: base(connection)
		{
		}

		public MockQuery(MockDbConnection connection, string script) 
			: base(connection, script)
		{
		}

		protected override string DefaultParameterFormat => ":{0}";

	    public new MockDbCommand CreateCommand()
	    {
	        return base.CreateCommand() as MockDbCommand;
	    }
	}

	public class MockDbConnection : DbConnection
	{
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
		public override void Close() => throw new NotImplementedException();
		public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
		public override void Open() => throw new NotImplementedException();
		public override string ConnectionString { get; set; }
		public override string Database => null;
		public override ConnectionState State => ConnectionState.Open;
		public override string DataSource => null;
		public override string ServerVersion => null;
		protected override DbCommand CreateDbCommand() => new MockDbCommand();
	}

	public class MockDbCommand : DbCommand
	{
		private MockDbParameters _parameters = new MockDbParameters();
		public override void Prepare() => throw new NotImplementedException();
		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }
		protected override DbConnection DbConnection { get; set; }
		protected override DbParameterCollection DbParameterCollection => _parameters;
		protected override DbTransaction DbTransaction { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override void Cancel() => throw new NotImplementedException();
		protected override DbParameter CreateDbParameter() => new MockDbParameter();
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
		public override int ExecuteNonQuery() => throw new NotImplementedException();
		public override object ExecuteScalar() => throw new NotImplementedException();
	}

	public class MockDbParameters : DbParameterCollection
	{
		private List<object> _list = new List<object>();

		public override int Add(object value)
		{
			_list.Add(value);
			return _list.Count - 1;
		}

		public override bool Contains(object value) => _list.Contains(value);
		public override void Clear() => _list.Clear();
		public override int IndexOf(object value) => _list.IndexOf(value);
		public override void Insert(int index, object value) => _list.Insert(index, value);
		public override void Remove(object value) => _list.Remove(value);
		public override void RemoveAt(int index) => _list.RemoveAt(index);
		public override void RemoveAt(string parameterName) => _list.RemoveAt(IndexOf(parameterName));
		protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
		protected override void SetParameter(string parameterName, DbParameter value) => _list[IndexOf(parameterName)] = value;
		public override int Count => _list.Count;

		public override object SyncRoot => new object();
		public override bool IsFixedSize => false;
		public override bool IsReadOnly => false;
		public override bool IsSynchronized => false;

		public override int IndexOf(string parameterName)
		{
			var param = _list.Cast<DbParameter>().FirstOrDefault(p => p.ParameterName == parameterName);
			return param == null ? -1 : _list.IndexOf(param);
		}

		public override IEnumerator GetEnumerator() => _list.GetEnumerator();
		protected override DbParameter GetParameter(int index) => _list[index] as DbParameter;
		protected override DbParameter GetParameter(string parameterName) => this[IndexOf(parameterName)];
		public override bool Contains(string value) => _list.Contains(value);
		public override void CopyTo(Array array, int index) => throw new NotImplementedException();
		public override void AddRange(Array values) => _list.AddRange(new[] { values });
	}

	public class MockDbParameter : DbParameter
	{
		public override void ResetDbType() => throw new NotImplementedException();
		public override DbType DbType { get; set; }
		public override ParameterDirection Direction { get; set; }
		public override bool IsNullable { get; set; }
		public override string ParameterName { get; set; }
		public override string SourceColumn { get; set; }
		public override DataRowVersion SourceVersion { get; set; }
		public override object Value { get; set; }
		public override bool SourceColumnNullMapping { get; set; }
		public override int Size { get; set; }
	}
}
