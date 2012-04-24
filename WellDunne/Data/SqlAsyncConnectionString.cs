using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Data.SqlClient;

namespace System.Data.Async
{
    /// <summary>
    /// Represents a connection string that is guaranteed to have been altered to enable asynchronous processing.
    /// </summary>
    public sealed class SqlAsyncConnectionString
    {
        internal readonly string _connectionString;

        /// <summary>
        /// Creates a connection string prepared for enabling asynchronous processing with an optional connection timeout.
        /// </summary>
        /// <param name="connectionString">The initial connection string to modify.</param>
        /// <param name="connectTimeout">Time to wait for a connection to open (in seconds). Default is 5.</param>
        public SqlAsyncConnectionString(string connectionString, [Optional] int? connectTimeout)
        {
            // Make sure asynchronous processing is enabled for the connection string:
            var csb = new SqlConnectionStringBuilder(connectionString);
            csb.AsynchronousProcessing = true;
            csb.ConnectTimeout = connectTimeout ?? 5;

            _connectionString = csb.ConnectionString;
        }

        /// <summary>
        /// Creates a connection string prepared for enabling asynchronous processing with a default connection timeout of 5 seconds.
        /// </summary>
        /// <param name="connectionString">The initial connection string to modify.</param>
        public SqlAsyncConnectionString(string connectionString)
            : this(connectionString, null)
        {
        }
    }
}
