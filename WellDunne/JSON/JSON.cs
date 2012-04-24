using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class JSON
    {
        private static readonly Newtonsoft.Json.JsonSerializer _json = Newtonsoft.Json.JsonSerializer.Create(
            new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

        public static StringBuilder SerializeToStringBuilder(StringBuilder sb, object graph)
        {
            using (var sw = new System.IO.StringWriter(sb))
            using (var jtw = new Newtonsoft.Json.JsonTextWriter(sw))
                _json.Serialize(jtw, graph);
            return sb;
        }

        public static System.IO.TextWriter SerializeToTextWriter(System.IO.TextWriter tw, object graph)
        {
            using (var jtw = new Newtonsoft.Json.JsonTextWriter(tw))
                _json.Serialize(jtw, graph);
            return tw;
        }

        public static string SerializeToString(object graph)
        {
            StringBuilder sb = new StringBuilder();
            return SerializeToStringBuilder(sb, graph).ToString();
        }
    }
}
