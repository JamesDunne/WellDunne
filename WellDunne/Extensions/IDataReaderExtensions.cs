using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace System.Data
{
    public static class IDataReaderExtensions
    {
        public static IEnumerable<AutoDataRecord> Enumerate(this IDataReader dr)
        {
            while (dr.Read()) yield return new AutoDataRecord(dr);
        }
    }
}
