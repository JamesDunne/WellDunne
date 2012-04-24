using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace System.Data.Async
{
    public delegate T ReadResultDelegate<T>(SqlParameterCollection prms, IEnumerable<AutoDataRecord> rows);
}
