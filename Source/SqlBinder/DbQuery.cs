using System;
using System.Collections.Generic;
using System.Data;

namespace SqlBinder
{
    /// <summary>
    /// Provides capability to create ADO.Net commands out of SqlBinder scripts.
    /// </summary>
    public class DbQuery : Query
    {
        /// <summary>
        /// Gets or sets a data connection which will be used to create commands and command parameters.
        /// </summary>
        public IDbConnection DataConnection { get; set; }

        /// <summary>
        /// Creates an ADO.Net query based on provided (open) connection.
        /// </summary>
        /// <param name="connection">Open connection (any DBMS).</param>
        public DbQuery(IDbConnection connection)
        {
            DataConnection = connection;
        }

        /// <summary>
        /// Creates an ADO.Net query based on provided (open) connection and a given SqlBinder script.
        /// </summary>
        /// <param name="connection">Open connection (any DBMS).</param>
        /// <param name="script">Your SqlBinder script.</param>
        public DbQuery(IDbConnection connection, string script)
            : base(script)
        {
            DataConnection = connection;
        }

        /// <summary>
        /// Basic, assumed, ADO.Net type mappings. This can be overriden on any level (Query, ConditionValue).
        /// </summary>
        private static Dictionary<Type, DbType> _dbTypeMap { get; } =
            new()
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(byte[])] = DbType.Binary,
                [typeof(object)] = DbType.Object,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time
            };

        /// <summary>
        /// When overriden in a derived class it allows customizing ADO DbType mapping based on clr types.
        /// </summary>
        protected virtual DbType OnResolveDbType(Type clrType)
        {
            var clrTypeNN = Nullable.GetUnderlyingType(clrType) ?? clrType;
            return _dbTypeMap.TryGetValue(clrTypeNN, out var v) ? v : DbType.Object;
        }

        /// <summary>
        /// Gets a <see cref="IDbCommand"/> associated with this query.
        /// </summary>
        public IDbCommand DbCommand { get; private set; }

        /// <summary>
        /// Delegate that can be used to intercept and alter command parameters on the fly. Use this to pass custom DBMS parameters. This is called
        /// before the parameter is added to command so you can either return the same reference or create your own.
        /// </summary>
        public Func<IDbDataParameter, IDbDataParameter> PrepareCommandParameter = p => p;

        /// <summary>
        /// Processes the script and creates a <see cref="IDbCommand"/> command.
        /// </summary>
        public virtual IDbCommand CreateCommand()
        {
            DbCommand = DataConnection.CreateCommand();
            DbCommand.CommandType = CommandType.Text;
            DbCommand.CommandText = GetSql();
            return DbCommand;
        }

        /// <summary>
        /// Adds a parameter to the output command, parameter type will be resolved by the virtual OnResolveDbType method.
        /// </summary>
        public override void AddSqlParameter(string paramName, object paramValue)
        {
            var param = DbCommand.CreateParameter();

            param.Direction = ParameterDirection.Input;

            if (paramValue == null)
                param.DbType = DbType.Object;
            else
            {
                param.DbType = OnResolveDbType(paramValue.GetType());

                if (paramValue is char)
                    param.Size = 1;
            }

            param.Value = paramValue ?? DBNull.Value;
            param.ParameterName = paramName;

            param = PrepareCommandParameter(param);

            DbCommand.Parameters.Add(param);

            base.AddSqlParameter(paramName, paramValue);
        }
    }
}