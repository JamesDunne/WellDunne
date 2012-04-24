using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace System.Data
{
    /// <summary>
    /// A simple wrapper around an <see cref="IDataRecord"/> that auto-increments the column ordinal on each successive function call to get a column value.
    /// </summary>
    /// <example>
    /// from row in (IEnumerable&lt;AutoDataRecord&gt;)rows
    /// select new {
    ///   a = row.Int32(),
    ///   b = row.Int32(),
    ///   c = row.String()
    /// }
    /// </example>
    public struct AutoDataRecord
    {
        private readonly IDataRecord _row;
        private int _ord;

        public AutoDataRecord(IDataRecord row)
        {
            _row = row;
            _ord = 0;
        }

        public Int32 Int32() { int i = _ord++; return _row.GetInt32(i); }
        public Int32? NullInt32() { int i = _ord++; return _row.IsDBNull(i) ? (Int32?)null : _row.GetInt32(i); }
        public Int64 Int64() { int i = _ord++; return _row.GetInt64(i); }
        public Int64? NullInt64() { int i = _ord++; return _row.IsDBNull(i) ? (Int64?)null : _row.GetInt64(i); }
        public String String() { int i = _ord++; return _row.GetString(i); }
        public String NullString() { int i = _ord++; return _row.IsDBNull(i) ? (String)null : _row.GetString(i); }
        public String StringTrim() { int i = _ord++; return _row.GetString(i).Trim(); }
        public String NullStringTrim() { int i = _ord++; return _row.IsDBNull(i) ? (String)null : _row.GetString(i).Trim(); }
        public Boolean Boolean() { int i = _ord++; return _row.GetBoolean(i); }
        public Boolean? NullBoolean() { int i = _ord++; return _row.IsDBNull(i) ? (Boolean?)null : _row.GetBoolean(i); }
        public DateTime DateTime() { int i = _ord++; return _row.GetDateTime(i); }
        public DateTime? NullDateTime() { int i = _ord++; return _row.IsDBNull(i) ? (DateTime?)null : _row.GetDateTime(i); }
        public DateTimeOffset DateTimeOffset() { int i = _ord++; return (_row as SqlDataReader).GetDateTimeOffset(i); }
        public DateTimeOffset? NullDateTimeOffset() { int i = _ord++; return _row.IsDBNull(i) ? (DateTimeOffset?)null : (_row as SqlDataReader).GetDateTimeOffset(i); }

        // TODO(jsd): More types!
    }
}
