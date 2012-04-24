using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WellDunne.Asynchrony;
using System.IO;

namespace WellDunne.Web
{
    public abstract class AsyncRESTHandler : AsyncContextHandler
    {
        public override void HandleRequestAsync(HttpContext http, AsyncContext work)
        {
            try
            {
                // NOTE(jsd): Calling `work.Done()` will mark the request as completed in the event that no other async work has been done.
                var req = CreateRequestHandler(http, work);
                
                if (req == null)
                {
                    work.Done();
                    return;
                }

                req.ProcessRequest();
            }
            catch (Exception ex)
            {
                http.Response.Clear();
                http.Response.StatusCode = 500;
                http.Response.Output.WriteLine(ex.ToString());
                http.Response.End();
                work.Done();
            }
        }
        
        public static void WriteJSONResponse(HttpRequest req, HttpResponse rsp, object graph)
        {
            string callback = req.QueryString["callback"];

            rsp.ContentEncoding = UTF8.EncodingNoBOM;

            // TODO(jsd): Set ContentType to "text/javascript" if callback != null?
            rsp.ContentType = "application/json";

            // JSONP support:
            if (callback != null) rsp.Output.Write(callback + "(");
            JSON.SerializeToTextWriter(rsp.Output, graph);
            if (callback != null) rsp.Output.Write(");");
        }

        protected abstract RESTRequest CreateRequestHandler(HttpContext http, AsyncContext work);
    }
}
