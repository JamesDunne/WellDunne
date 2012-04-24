using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Data.Async
{
    /// <summary>
    /// Represents a SQL command that can be easily executed synchronously.
    /// </summary>
    public sealed class SqlSyncCommand
    {
        #region Private fields

        private readonly SqlConnection _db;
        private readonly SqlCommand _cmd;

        /// <summary>
        /// Default exception handler. Override with SetExceptionHandler.
        /// </summary>
        private static readonly Action<SqlSyncCommand, Exception> _errDefault = (cmd, ex) =>
        {
            Trace.WriteLine(ex.ToString(), "SqlSyncCommand");
        };

        #endregion

        #region Constructors

        private SqlSyncCommand(CommandType commandType, string commandText, string connectionString, int? commandTimeout)
        {
            this._db = new SqlConnection(connectionString);

            // Create a command to execute the query with:
            this._cmd = _db.CreateCommand();
            this._cmd.CommandTimeout = commandTimeout ?? 60;
            this._cmd.CommandText = commandText;
            this._cmd.CommandType = commandType;
        }

        #endregion

        #region Static creator methods

        /// <summary>
        /// Creates an asynchronous command to execute an inline SQL query.
        /// </summary>
        /// <param name="queryText">The inline SQL query text to execute</param>
        /// <param name="connectionString">The connection string</param>
        /// <returns></returns>
        public static SqlSyncCommand Query(string queryText, string connectionString)
        {
            return Query(queryText, connectionString, null);
        }

        /// <summary>
        /// Creates an asynchronous command to execute an inline SQL query with an optional execution timeout.
        /// </summary>
        /// <param name="queryText">The inline SQL query text to execute</param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="commandTimeout">An optional command execution timeout value (in seconds). Default is 60.</param>
        /// <returns></returns>
        public static SqlSyncCommand Query(string queryText, string connectionString, [Optional] int? commandTimeout)
        {
            return new SqlSyncCommand(CommandType.Text, queryText, connectionString, commandTimeout);
        }

        /// <summary>
        /// Creates an asynchronous command to execute a SQL stored procedure.
        /// </summary>
        /// <param name="procedureName">The SQL stored procedure name to execute</param>
        /// <param name="connectionString">The connection string</param>
        /// <returns></returns>
        public static SqlSyncCommand StoredProcedure(string procedureName, string connectionString)
        {
            return StoredProcedure(procedureName, connectionString, null);
        }

        /// <summary>
        /// Creates an asynchronous command to execute a SQL stored procedure with an optional execution timeout.
        /// </summary>
        /// <param name="procedureName">The SQL stored procedure name to execute</param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="commandTimeout">An optional command execution timeout value (in seconds). Default is 60.</param>
        /// <returns></returns>
        public static SqlSyncCommand StoredProcedure(string procedureName, string connectionString, [Optional] int? commandTimeout)
        {
            return new SqlSyncCommand(CommandType.StoredProcedure, procedureName, connectionString, commandTimeout);
        }

        #endregion

        #region Query parameters

        /// <summary>
        /// Adds a command parameter with a name and value.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithValue(string paramName, object value)
        {
            var prm = this._cmd.Parameters.AddWithValue(paramName, value);
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name and type.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithType(string paramName, SqlDbType type)
        {
            var prm = this._cmd.Parameters.Add(paramName, type);
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, and direction.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="direction">Parameter direction</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeDirection(string paramName, SqlDbType type, ParameterDirection direction)
        {
            var prm = this._cmd.Parameters.Add(paramName, type);
            prm.Direction = direction;
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, and value.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="value">Parameter value</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeValue(string paramName, SqlDbType type, object value)
        {
            var prm = this._cmd.Parameters.Add(paramName, type);
            prm.Value = value;
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, direction, and value.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="direction">Parameter direction</param>
        /// <param name="value">Parameter value</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeDirectionValue(string paramName, SqlDbType type, ParameterDirection direction, object value)
        {
            var prm = this._cmd.Parameters.Add(paramName, type);
            prm.Direction = direction;
            prm.Value = value;
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, and size.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="size">Parameter size</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeSize(string paramName, SqlDbType type, int size)
        {
            var prm = this._cmd.Parameters.Add(paramName, type, size);
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, size, and direction.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="size">Parameter size</param>
        /// <param name="direction">Parameter direction</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeSizeDirection(string paramName, SqlDbType type, int size, ParameterDirection direction)
        {
            var prm = this._cmd.Parameters.Add(paramName, type, size);
            prm.Direction = direction;
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, size, and value.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="size">Parameter size</param>
        /// <param name="value">Parameter value</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeSizeValue(string paramName, SqlDbType type, int size, object value)
        {
            var prm = this._cmd.Parameters.Add(paramName, type, size);
            prm.Value = value;
            return prm;
        }

        /// <summary>
        /// Adds a command parameter with a name, type, size, direction, and value.
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="type">Parameter type</param>
        /// <param name="size">Parameter size</param>
        /// <param name="direction">Parameter direction</param>
        /// <param name="value">Parameter value</param>
        /// <returns></returns>
        public SqlParameter AddParameterWithTypeSizeDirectionValue(string paramName, SqlDbType type, int size, ParameterDirection direction, object value)
        {
            var prm = this._cmd.Parameters.Add(paramName, type, size);
            prm.Direction = direction;
            prm.Value = value;
            return prm;
        }

        #endregion

        #region Private

        /// <summary>
        /// Enumerates a SqlDataReader, wrapping each row in an <see cref="AutoDataRecord"/> struct.
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        private static IEnumerable<AutoDataRecord> _enumerate(SqlDataReader dr)
        {
            try
            {
                while (dr.Read()) yield return new AutoDataRecord(dr);
            }
            finally
            {
                dr.Close();
            }
        }

        #endregion

        #region Command properties

        /// <summary>
        /// Gets the command parameter collection.
        /// </summary>
        public SqlParameterCollection Parameters { get { return _cmd.Parameters; } }

        /// <summary>
        /// Gets the command text.
        /// </summary>
        public string CommandText { get { return _cmd.CommandText; } }

        /// <summary>
        /// Gets the command type.
        /// </summary>
        public CommandType CommandType { get { return _cmd.CommandType; } }

        #endregion

        #region Synchronous execution

        #region ExecuteReader

        public void ExecuteReader<T, Tstate>(Action<SqlSyncCommand, Exception> err, Func<SqlParameterCollection, IEnumerable<AutoDataRecord>, T> readResult, Action<T, Tstate> useResult, Tstate state)
        {
            var errorHandler = err ?? _errDefault;
            try
            {
                this._db.Open();

                T result;
                using (var dr = this._cmd.ExecuteReader(CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess))
                    result = readResult(this._cmd.Parameters, _enumerate(dr));

                this._cmd.Dispose();
                this._db.Close();

                useResult(result, state);
            }
            catch (Exception ex)
            {
                this._cmd.Dispose();
                this._db.Close();

                errorHandler(this, ex);
            }
        }

        #endregion

        #region ExecuteNonQuery<T>

        public void ExecuteNonQuery<T, Tstate>(Action<SqlSyncCommand, Exception> err, Func<SqlParameterCollection, int, T> processResult, Action<T, Tstate> useResult, Tstate state)
        {
            var errorHandler = err ?? _errDefault;
            try
            {
                this._db.Open();

                int nr = this._cmd.ExecuteNonQuery();
                T result;
                result = processResult(this._cmd.Parameters, nr);

                this._cmd.Dispose();
                this._db.Close();

                useResult(result, state);
            }
            catch (Exception ex)
            {
                this._cmd.Dispose();
                this._db.Close();

                errorHandler(this, ex);
            }
        }

        #endregion

        #region ExecuteNonQuery

        public void ExecuteNonQuery<Tstate>(Action<SqlSyncCommand, Exception> err, Action<Tstate> done, Tstate state)
        {
            var errorHandler = err ?? _errDefault;
            try
            {
                this._db.Open();

                this._cmd.ExecuteNonQuery();

                this._cmd.Dispose();
                this._db.Close();

                done(state);
            }
            catch (Exception ex)
            {
                this._cmd.Dispose();
                this._db.Close();

                errorHandler(this, ex);
            }
        }

        #endregion

        #endregion
    }
}
