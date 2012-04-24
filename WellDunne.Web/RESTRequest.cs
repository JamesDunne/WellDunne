using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WellDunne.Asynchrony;

namespace WellDunne.Web
{
    public abstract class RESTRequest
    {
        protected readonly AsyncRESTHandler handler;
        protected readonly HttpContext http;
        protected readonly AsyncContext work;

        protected readonly HttpRequest req;
        protected readonly HttpResponse rsp;

        protected readonly string url;
        protected readonly string[] route;

        private static readonly char[] _splitChars = new char[1] { '/' };

        protected RESTRequest(AsyncRESTHandler handler, HttpContext http, AsyncContext work)
        {
            this.handler = handler;
            this.http = http;
            this.work = work;
            this.req = http.Request;
            this.rsp = http.Response;

            // Remove the /path/to/file.ashx prefix from the request URL:
            string basePath = req.ApplicationPath.RemoveIfEndsWith("/") + req.AppRelativeCurrentExecutionFilePath.Substring(1);
            url = req.Url.AbsolutePath.RemoveIfStartsWith(basePath, StringComparison.OrdinalIgnoreCase).RemoveIfStartsWith("/", StringComparison.Ordinal);

            // Split the url by '/' into route portions:
            route = url.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
            // Unescape route parts:
            for (int i = 0; i < route.Length; ++i)
                route[i] = Uri.UnescapeDataString(route[i]);
        }

        /// <summary>
        /// Begin processing the REST request asynchronously.
        /// </summary>
        public abstract void ProcessRequest();

        protected void outputJSON(object graph)
        {
            AsyncRESTHandler.WriteJSONResponse(req, rsp, graph);
        }

        protected bool testRoute(int requiredPartCount, params string[] parts)
        {
            if (route.Length != requiredPartCount) return false;
            if (parts == null) return true;

            for (int i = 0; i < parts.Length; ++i)
            {
                if (parts[i] == null) continue;
                if (!String.Equals(route[i], parts[i], StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }
    }
}
