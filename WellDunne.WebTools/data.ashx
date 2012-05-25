<%@ WebHandler Language="C#" Class="WellDunne.WebTools.DataServiceProvider" %>
<%@ Assembly Name="System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="System.Data.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="WellDunne" %>

// Uncomment to require HTTP Basic authentication
//#define RequireAuth
// Uncomment to require authentication to read data
#define RequireAuthToRead
// Allow JSONP requests
#define JSONP

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WellDunne.WebTools
{
    /// <summary>
    /// Main IHttpHandler for the tool.
    /// </summary>
    public class DataServiceProvider : IHttpHandler
    {
#if RequireAuth
        private const string httpBasicAuthUsername = "admin";
        private const string httpBasicAuthPassword = "admin";
#if RequireAuthToRead
        private const string httpBasicAuth_ReadOnly_Username = "guest";
        private const string httpBasicAuth_ReadOnly_Password = "guest";
#endif
#endif

        private const string helpText =
@"RESTful data web service tool with basic CRUD operation support and limited querying ability.
James S. Dunne
2012-05-10

NOTE: All routes are relative to the virtual path of data.ashx:
e.g. ~/data.ashx/SMSProvider?$skip=5&$top=10

NOTE: Only GET and POST verbs are supported due to default limitations of IIS and its handling of verbs.

Routes:
  * GET  /                      lists all entity names supported
  * GET  /entity                lists all records (queried by $filter)
  * GET  /entity(id)            gets one record by id

  * POST /entity
 or POST /entity/insert         inserts a set of records

  * POST /entity/update         updates a set of records (queried by $filter)
  * POST /entity/delete         deletes a set of records (queried by $filter)

  * POST /entity(id)
 or POST /entity(id)/update     updates a single record by id

  * POST /entity(id)/delete     deletes a single record by id

Query filters (order matters):
  * GET  /entity
      * $skip=n                             - Skip N rows
      * $top=n                              - Take top N rows
      * $orderby=(name) (direction),...     - Order by column(s) in direction(s)
      * $filter=(name) (op) (value)         - Filter rows via simple comparison operations
            name:       name that identifies the property to compare (case insensitive)
            op:         operator name
                eq   (==): equals
                ne   (!=): not equals
                lt   (< ): less than
                le   (<=): less than or equal to
                gt   (> ): greater than
                ge   (>=): greater than or equal to
                like:      SQL LIKE filter
            value:      a value to compare to
                strings values are enclosed in single-quotes and backslash character escaping rules apply.

    e.g. /entity?$skip=5&$top=10
         /entity?$filter=Code eq 'HELLO'
         /entity?$filter=Comment eq 'Hello\nWorld!\tHow are you? I\'m good.'
         /entity?$filter=Price le 19.95";

        public bool IsReusable { get { return true; } }

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="ctx"></param>
        public void ProcessRequest(HttpContext ctx)
        {
            HttpRequest req = ctx.Request;
            HttpResponse rsp = ctx.Response;

            // All results return JSON:
            var result = new JsonResult((object)null);

            // Create a JsonTextWriter to write directly to the HTTP response stream:
            var jrsp = new JsonTextWriter(rsp.Output);

            // Keep a list of disposable resources that must be reclaimed after the response is written:
            List<IDisposable> disposables = new List<IDisposable>();

            // Execute the request:
            result = main(req, disposables);

            // All JSON output is serialized at this point:
            rsp.StatusCode = result.statusCode;

            // JSONP support here:
#if JSONP
            string callback = req.QueryString["callback"];
            if (callback != null)
            {
                // Make sure callback is an identifier:
                if (!callback.IsIdentifier())
                {
                    callback = null;
                    result = new JsonResult(400, "Invalid callback query string parameter");
                    goto output;
                }

                // Use javascript content-type and write the callback prefix:
                rsp.ContentType = "application/javascript";
                rsp.Write(callback + "(");
            }
            else
            {
#endif
                // Normal JSON data:
                rsp.ContentType = "application/json";
#if JSONP
            }

        output:
#endif

            try
            {
                // Serialize the result object directly to the response stream:
                json.Serialize(jrsp, result);
            }
            catch (Exception ex)
            {
                json.Serialize(jrsp, formatException(ex));
            }

#if JSONP
            // Close the callback expression for JSONP:
            if (callback != null) rsp.Write(");");
#endif

            // Dispose resources that were left open for streaming JSON serialization:
            foreach (IDisposable d in disposables)
            {
                try { if (d != null) d.Dispose(); }
                catch { }
            }

            disposables = null;
        }

        /// <summary>
        /// Handles executing the request and producing a JSON result for success or error.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="disposables"></param>
        /// <returns></returns>
        private static JsonResult main(HttpRequest req, List<IDisposable> disposables)
        {
            try
            {
                string requestIP = req.ServerVariables["REMOTE_ADDR"];

                // PRODUCTION: 10.71.4.100 is the back-side of the F5 load balancer.
                if ((requestIP == "10.71.4.100") ||
                    (!requestIP.StartsWith("10.") && (requestIP != "127.0.0.1") && (requestIP != "::1")))
                {
                    // Disallow access to external IPs:
                    return new JsonResult(403, "Forbidden");
                }

                // Basic auth support:
#if RequireAuth
#if RequireAuthToRead
                string auth = req.Headers["Authorization"];
                if (auth == null) return new JsonResult(401, "Unauthorized");
                if (!auth.StartsWith("Basic ")) return new JsonResult(401, "Unauthorized");

                string b64up = auth.Substring(6);
                if (b64up == Convert.ToBase64String(Encoding.ASCII.GetBytes(httpBasicAuth_ReadWrite_Username + ":" + httpBasicAuth_ReadWrite_Password)))
                {
                    // Read/Write access.
                    // Do nothing here to prevent access.
                    goto authorized;
                }
                else if (b64up == Convert.ToBase64String(Encoding.ASCII.GetBytes(httpBasicAuth_ReadOnly_Username + ":" + httpBasicAuth_ReadOnly_Password)))
                {
                    // Read-only access.
                    goto readOnly;
                }
                else
                {
                    // No access.
                    return new JsonResult(401, "Unauthorized");
                }
#else
                // Authorization is not required for read-only access:
                string auth = req.Headers["Authorization"];
                if (auth == null) goto readOnly;
                if (!auth.StartsWith("Basic ")) goto readOnly;

                string b64up = auth.Substring(6);
                // Authorization is required for read-write access:
                if (b64up == Convert.ToBase64String(Encoding.ASCII.GetBytes(httpBasicAuth_ReadWrite_Username + ":" + httpBasicAuth_ReadWrite_Password)))
                {
                    // Read/write is authorized.
                    goto authorized;
                }
                else
                {
                    // Read-only mode.
                    goto readOnly;
                }
#endif

            readOnly:
                if (!req.HttpMethod.CaseInsensitiveTrimmedEquals("GET"))
                    return new JsonResult(401, "Unauthorized; read-only access");

            authorized:
#endif

                return execute(req, disposables);
            }
            catch (Exception ex)
            {
                // Catch exceptions and spit them out in JSON, sometimes customized depending on the type:
                return formatException(ex);
            }
        }

        private static readonly char[] _splitChars = new char[1] { '/' };

        private static bool doCommit(HttpRequest req)
        {
            return req.QueryString.AllKeys.Contains("_commit", StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Main request handler.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="disposables"></param>
        /// <returns></returns>
        private static JsonResult execute(HttpRequest req, List<IDisposable> disposables)
        {
            // Remove the /path/to/file.ashx prefix from the request URL:
            string basePath = req.ApplicationPath.RemoveIfEndsWith("/") + req.AppRelativeCurrentExecutionFilePath.Substring(1);
            string url = req.Url.AbsolutePath.RemoveIfStartsWith(basePath, StringComparison.OrdinalIgnoreCase).RemoveIfStartsWith("/", StringComparison.Ordinal);

            // Split the url by '/' into route portions:
            string[] route = url.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
            // Unescape route parts:
            for (int i = 0; i < route.Length; ++i)
                route[i] = Uri.UnescapeDataString(route[i]);

            if (route.Length == 0)
            {
                // GET /

                // Default route to give some basic information:
                return new JsonResult((object)new
                {
                    // Show the list of entities available:
                    entities = EntityRegistrar.Entities.Keys,
                    // Add some help documentation:
                    helpText = helpText.RemoveAllCharacters('\r').Split('\n')
                });
            }

            // Parse "Entity(id)":
            int idxpo, idxpc;
            string entity = route[0];
            string id = null;
            if ((idxpo = entity.IndexOf('(')) != -1)
            {
                if ((idxpc = entity.LastIndexOf(')')) != -1)
                {
                    // Parse the id out:
                    id = entity.Substring(idxpo + 1, idxpc - (idxpo + 1));
                    entity = entity.Substring(0, idxpo);
                }
                else
                {
                    return new JsonResult(400, String.Format("Bad route"));
                }
            }

            // Get the repository factory method for the entity name:
            Func<ISimpleDataProvider> getdb;
            if (!EntityRegistrar.Entities.TryGetValue(entity, out getdb))
                return new JsonResult(400, String.Format("Unknown entity '{0}'", route[0]));

            ISimpleDataProvider db = getdb();

            // Dispose of the IDataProvider when the response is serialized:
            disposables.Add(db);

            // NOTE(jsd): No default support in IIS7.5 for HTTP methods other than GET and POST.

            if (route.Length == 1)
            {
                // VERB /{entity}(id?)

                if (req.HttpMethod.CaseInsensitiveTrimmedEquals("GET"))
                {
                    // GET /{entity}(id?)

                    if (id != null)
                    {
                        // GET /{entity}(id)
                        return getEntityByID(db, req, id);
                    }
                    else
                    {
                        // GET /{entity}
                        return getEntities(db, req);
                    }
                }
                else
                {
                    // POST /{entity}(id?)

                    if (id != null)
                    {
                        // POST /{entity}(id)
                        return updateByID(db, req, id);
                    }
                    else
                    {
                        // POST /{entity}
                        return insertEntities(db, req);
                    }
                }
            }
            else if (route.Length == 2)
            {
                // VERB /{entity}(id?)/{action}
                string action = route[1];

                if (!req.HttpMethod.CaseInsensitiveTrimmedEquals("GET"))
                {
                    // POST /{entity}(id?)/{action}
                    if (id != null)
                    {
                        // POST /{entity}(id)/{action}
                        if (action.CaseInsensitiveTrimmedEquals("update"))
                        {
                            // POST /{entity}(id)/update
                            return updateByID(db, req, id);
                        }
                        else if (action.CaseInsensitiveTrimmedEquals("delete"))
                        {
                            // POST /{entity}(id)/delete
                            return deleteByID(db, req, id);
                        }
                    }
                    else
                    {
                        // POST /{entity}/{action}
                        if (action.CaseInsensitiveTrimmedEquals("insert"))
                        {
                            // POST /{entity}/insert
                            return insertEntities(db, req);
                        }
                        else if (action.CaseInsensitiveTrimmedEquals("update"))
                        {
                            // POST /{entity}/update
                            return updateByQuery(db, req);
                        }
                        else if (action.CaseInsensitiveTrimmedEquals("delete"))
                        {
                            // POST /{entity}/delete
                            return deleteByQuery(db, req);
                        }
                    }
                }
            }

            return new JsonResult(400, "Unknown route");
        }

        private static Either<JsonResult, IQueryable> buildQuery(ISimpleDataProvider db, HttpRequest req)
        {
            // TODO(jsd): Add more querying support on `IQueryable`.
            IQueryable query = db.Query();
            int take = 1000;

            // Process query-string filters in order: (e.g. $skip before $top)
            var q = req.QueryString;
            bool isOrdered = false;

            for (int i = 0; i < q.Count; ++i)
            {
                string qkey = q.Keys[i];

                // NOTE(jsd): Unfortunately, ASP.NET unifies duplicate keys into a single key and gives the values in order without respect
                // to keys that might come in between the duplicates.
                foreach (string qvalue in q.GetValues(i))
                {
                    // All filters start with '$':
                    if (qkey == null) continue;
                    if (qkey[0] != '$') continue;

                    string filter = qkey.Substring(1);

                    // Apply the filter operator:
                    if (filter.CaseInsensitiveTrimmedEquals("filter"))
                    {
#if true
                        var parser = new ExpressionLibrary.Parser(new ExpressionLibrary.Lexer(new StringReader(qvalue)));
                        ExpressionLibrary.Expression filterExpr;
                        if (!parser.ParseExpression(out filterExpr))
                        {
                            var parserErrors = parser.GetErrors();
                            if (parserErrors.Count == 0)
                            {
                                return new JsonResult(400, "Bad filter expression");
                            }
                            else if (parserErrors.Count == 1)
                            {
                                return new JsonResult(
                                    400,
                                    String.Format("Error in filter expression at position {0}: {1}", parserErrors[0].Token.Position + 1, parserErrors[0].Message),
                                    (object)parserErrors.SelectAsList(pe => String.Format("error(at {0}): {1}", pe.Token.Position + 1, pe.Message))
                                );
                            }
                            else
                            {
                                return new JsonResult(
                                    400,
                                    "Multiple errors in filter expression",
                                    (object)parserErrors.SelectAsList(pe => String.Format("error(at {0}): {1}", pe.Token.Position + 1, pe.Message))
                                );
                            }
                        }

                        // Translate the filter expression into a lambda for LINQ:
                        query = query.TranslateToExpression(filterExpr);
#else
                        // TODO(jsd): This regex works only for simple `a op b` binary expression comparisons.
                        var match = System.Text.RegularExpressions.Regex.Match(
                            qvalue,
                            @"^([_a-zA-Z][_a-zA-Z0-9]*)\s*(eq|ne|lt|le|gt|ge|like)\s*('.*'|[0-9]+(?:\.?[0-9]+)?|true|false|null)$"
                        );
                        if (!match.Success) return new JsonResult(400, String.Format("Bad filter expression `{0}`", qvalue));

                        // NOTE(jsd): Apparently Groups[0] is the entire match so the capture groups start at [1].
                        string propertyName = match.Groups[1].Value;
                        string op = match.Groups[2].Value;
                        string comparand = match.Groups[3].Value;

                        if (comparand == "null") comparand = null;
                        else if (comparand[0] == '\'')
                        {
                            // Unescape the string:
                            if (comparand[comparand.Length - 1] != '\'') return new JsonResult(400, "Invalid string literal");

                            StringBuilder sb = new StringBuilder(comparand.Length);
                            for (int j = 1; j < comparand.Length - 1; ++j)
                            {
                                if (comparand[j] == '\\')
                                {
                                    if (++j >= comparand.Length - 1) return new JsonResult(400, "Non-terminated string escape sequence");
                                    switch (comparand[j])
                                    {
                                        case '\\': sb.Append('\\'); break;
                                        case 'n': sb.Append('\n'); break;
                                        case 'r': sb.Append('\r'); break;
                                        case 't': sb.Append('\t'); break;
                                        case '\'': sb.Append('\''); break;
                                        case '\"': sb.Append('\"'); break;
                                        default: return new JsonResult(400, String.Format("Unrecognized string escape sequence '\\{0}' at position {1}", comparand[j], j));
                                    }
                                }
                                else sb.Append(comparand[j]);
                            }

                            comparand = sb.ToString();
                        }

                        // NOTE(jsd): comparand is always a string type here but for a literal string value it must be a quoted string literal.

                        // Translate the two-character operator shorthand:
                        Extensions.BinaryOperator binop;
                        switch (op)
                        {
                            case "eq": binop = Extensions.BinaryOperator.Equal; break;
                            case "ne": binop = Extensions.BinaryOperator.NotEqual; break;
                            case "lt": binop = Extensions.BinaryOperator.LessThan; break;
                            case "le": binop = Extensions.BinaryOperator.LessThanOrEqual; break;
                            case "gt": binop = Extensions.BinaryOperator.GreaterThan; break;
                            case "ge": binop = Extensions.BinaryOperator.GreaterThanOrEqual; break;
                            case "like": binop = Extensions.BinaryOperator.SqlLike; break;
                            default: return new JsonResult(400, String.Format("Unknown comparison operator '{0}'", op));
                        }

                        query = query.FilterByComparison(
                            binop,
                            propertyName,
                            (type) => System.ComponentModel.TypeDescriptor.GetConverter(type).ConvertFromInvariantString(comparand)
                        );
#endif
                    }
                    else if (filter.CaseInsensitiveTrimmedEquals("skip"))
                    {
                        if (qvalue == null) continue;
                        int value;
                        if (!Int32.TryParse(qvalue, out value))
                            continue;

                        query = query.Skip(value);
                    }
                    else if (filter.CaseInsensitiveTrimmedEquals("top"))
                    {
                        if (qvalue == null) continue;
                        int value;
                        if (!Int32.TryParse(qvalue, out value))
                            continue;

                        // There can be only one "Take":
                        take = value;
                    }
                    else if (filter.CaseInsensitiveTrimmedEquals("orderby"))
                    {
                        if (qvalue == null) continue;
                        string[] exps = qvalue.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string exp in exps)
                        {
                            string name;
                            bool isAscending = true;

                            // Parse the orderby expression:
                            int spidx = exp.IndexOf(' ');
                            if (spidx == -1) name = exp;
                            else
                            {
                                name = exp.Substring(0, spidx);
                                string tmp = exp.Substring(spidx + 1);
                                if (tmp.CaseInsensitiveTrimmedEquals("asc"))
                                    isAscending = true;
                                else if (tmp.CaseInsensitiveTrimmedEquals("desc"))
                                    isAscending = false;
                                else
                                    return new JsonResult(400, String.Format("Bad orderby direction '{0}'; must be either 'asc' or 'desc'", tmp));
                            }

                            if (isAscending)
                            {
                                if (isOrdered)
                                    query = ((IOrderedQueryable)query).ThenBy(name);
                                else
                                {
                                    query = query.OrderBy(name);
                                    isOrdered = true;
                                }
                            }
                            else
                            {
                                if (isOrdered)
                                    query = ((IOrderedQueryable)query).ThenByDescending(name);
                                else
                                {
                                    query = query.OrderByDescending(name);
                                    isOrdered = true;
                                }
                            }
                        }
                    }
                    // TODO: $select - can't create anonymous types on-the-fly to contain the projection... or can we?
                    else
                    {
                        return new JsonResult(400, String.Format("Unknown filter name '{0}'", filter));
                    }
                }
            }

            // Force a max # of records to retrieve (negative values disable):
            if (take >= 0)
                query = query.Take(take);

            return new Either<JsonResult, IQueryable>(query);
        }

        private static JsonResult getEntityByID(ISimpleDataProvider db, HttpRequest req, string id)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            object ent = db.GetByID(id);
            sw.Stop();
            if (ent == null) return new JsonResult(404, "Could not find record by id");

            return new JsonResult((object)ent.AsArrayOrEmpty(), new JsonResultMeta
            {
                execMsec = sw.ElapsedMilliseconds,
                executed = DateTimeOffset.Now,
                server = db.ServerName,
                database = db.DatabaseName
            });
        }

        private static JsonResult getEntities(ISimpleDataProvider db, HttpRequest req)
        {
            return buildQuery(db, req).Collapse(
                jr => jr,
                // Must complete execution here for exception handling purposes:
                query =>
                {
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    List<object> results = query.Cast<object>().ToList();
                    sw.Stop();

                    return new JsonResult((object)results, new JsonResultMeta
                    {
                        execMsec = sw.ElapsedMilliseconds,
                        executed = DateTimeOffset.Now,
                        server = db.ServerName,
                        database = db.DatabaseName
                    });
                }
            );
        }

        private static JsonResultMeta submit(ISimpleDataProvider db, HttpRequest req)
        {
            if (doCommit(req))
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                db.Submit();
                sw.Stop();
                return new JsonResultMeta
                {
                    committed = true,
                    execMsec = sw.ElapsedMilliseconds,
                    executed = DateTimeOffset.Now,
                    server = db.ServerName,
                    database = db.DatabaseName
                };
            }
            else
            {
                return new JsonResultMeta
                {
                    committed = false,
                    execMsec = 0L,
                    executed = DateTimeOffset.Now,
                    server = db.ServerName,
                    database = db.DatabaseName
                };
            }
        }

        private static JsonResult updateByID(ISimpleDataProvider db, HttpRequest req, string id)
        {
            // UPDATE:
            var ent = db.UpdateByID(id, req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            if (ent == null) return new JsonResult(404, "Record to be updated could not be found by id");

            var meta = submit(db, req);

            return new JsonResult((object)ent.AsArrayOrEmpty(), (object)meta);
        }

        private static JsonResult updateByQuery(ISimpleDataProvider db, HttpRequest req)
        {
            return buildQuery(db, req).Collapse(
                jr => jr,
                query =>
                {
                    var updated = new ArrayList();
                    foreach (object ent in query)
                        updated.Add(ent);

                    // Update the retrieved entities with new values from the JSON body:
                    db.UpdateList(updated, req.InputStream, req.ContentEncoding);

                    var meta = submit(db, req);

                    return new JsonResult((object)updated, (object)meta);
                }
            );
        }

        private static JsonResult deleteByQuery(ISimpleDataProvider db, HttpRequest req)
        {
            return buildQuery(db, req).Collapse(
                jr => jr,
                query =>
                {
                    var deleted = new ArrayList();
                    foreach (object ent in query)
                        deleted.Add(ent);

                    // Update the retrieved entities with new values from the JSON body:
                    db.DeleteList(deleted);

                    var meta = submit(db, req);

                    return new JsonResult((object)deleted, (object)meta);
                }
            );
        }

        private static JsonResult insertEntities(ISimpleDataProvider db, HttpRequest req)
        {
            // INSERT:
            ArrayList newents = db.InsertList(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            if (newents == null) return new JsonResult(400, "No data provided to insert");

            var meta = submit(db, req);

            return new JsonResult((object)newents, (object)meta);
        }

        private static JsonResult deleteByID(ISimpleDataProvider db, HttpRequest req, string id)
        {
            object ent = db.DeleteByID(id);
            if (ent == null) return new JsonResult(404, "Record to be deleted could not be found by id");

            var meta = submit(db, req);

            return new JsonResult((object)ent.AsArrayOrEmpty(), (object)meta);
        }

        #region Utility and formatting methods

        private sealed class JsonResultMeta
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? committed;
            public long execMsec;
            public DateTimeOffset executed;
            public string server;
            public string database;
        }

        private struct JsonResult
        {
            [JsonIgnore]
            public readonly int statusCode;

            // NOTE(jsd): Fields are serialized to JSON in lexical definition order.
            public readonly bool success;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public readonly string message;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public readonly object errors;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public readonly object meta;

            // NOTE(jsd): `results` must be last.
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public readonly object results;

            public JsonResult(int statusCode, string failureMessage)
            {
                this.statusCode = statusCode;
                success = false;
                message = failureMessage;
                errors = null;
                results = null;
                meta = null;
            }

            public JsonResult(int statusCode, string failureMessage, object errorData)
            {
                this.statusCode = statusCode;
                success = false;
                message = failureMessage;
                errors = errorData;
                results = null;
                meta = null;
            }

            public JsonResult(object successfulResults)
            {
                statusCode = 200;
                success = true;
                message = null;
                errors = null;
                results = successfulResults;
                meta = null;
            }

            public JsonResult(object successfulResults, object metaData)
            {
                statusCode = 200;
                success = true;
                message = null;
                errors = null;
                results = successfulResults;
                meta = metaData;
            }
        }

        private sealed class IgnoreEntitySetsJsonContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

                // Remove properties which have type of `System.Data.Linq.EntitySet<T>`:
                properties = properties
                    .Where(p => !(p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(System.Data.Linq.EntitySet<>)))
                    .ToList();

                return properties;
            }
        }

        private static readonly JsonSerializer json = JsonSerializer.Create(new JsonSerializerSettings
        {
            // NOTE(jsd): Include null values in output for clarity.
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            // Ignore EntitySet<T> property types for serialization (these cause lazy data loading (slow) and circular serialization references (bad)):
            ContractResolver = new IgnoreEntitySetsJsonContractResolver()
        });

        private static JsonResult sqlError(System.Data.SqlClient.SqlException sqex)
        {
            int statusCode = 500;

            var errorData = new List<System.Data.SqlClient.SqlError>(sqex.Errors.Count);
            var msgBuilder = new StringBuilder(sqex.Message.Length);
            foreach (System.Data.SqlClient.SqlError err in sqex.Errors)
            {
                // Skip "The statement has been terminated.":
                if (err.Number == 3621) continue;

                errorData.Add(err);

                if (msgBuilder.Length > 0)
                    msgBuilder.AppendFormat("\n{0}", err.Message);
                else
                    msgBuilder.Append(err.Message);

                // Determine the HTTP status code to return:
                switch (sqex.Number)
                {
                    // Column does not allow NULLs.
                    case 515: statusCode = 400; break;
                    // Violation of UNIQUE KEY constraint '{0}'. Cannot insert duplicate key in object '{1}'.
                    case 2627: statusCode = 409; break;
                }
            }

            string message = msgBuilder.ToString();
            return new JsonResult(statusCode, message, errorData);
        }

        private static JsonResult formatException(Exception ex)
        {
            JsonException jex;
            JsonSerializationException jsex;
            System.Data.SqlClient.SqlException sqex;

            object innerException = null;
            if (ex.InnerException != null)
                innerException = (object)formatException(ex.InnerException);

            if ((jex = ex as JsonException) != null)
            {
                return new JsonResult(jex.StatusCode, jex.Message);
            }
            else if ((jsex = ex as JsonSerializationException) != null)
            {
                object errorData = new
                {
                    type = ex.GetType().FullName,
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException
                };

                return new JsonResult(500, jsex.Message, new[] { errorData });
            }
            else if ((sqex = ex as System.Data.SqlClient.SqlException) != null)
            {
                return sqlError(sqex);
            }
            else
            {
                object errorData = new
                {
                    type = ex.GetType().FullName,
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException
                };

                return new JsonResult(500, ex.Message, new[] { errorData });
            }
        }

        #endregion
    }

    /// <summary>
    /// Simplistic CRUD operation provider for a data entity.
    /// </summary>
    public interface ISimpleDataProvider : IDisposable
    {
        IQueryable Query();
        object GetByID(string id);
        ArrayList InsertList(System.IO.Stream inputStream, Encoding encoding);
        object UpdateByID(string id, System.IO.Stream inputStream, Encoding encoding);
        object DeleteByID(string id);
        void UpdateList(ArrayList entities, System.IO.Stream inputStream, Encoding encoding);
        void DeleteList(ArrayList entities);
        void Submit();
        string ServerName { get; }
        string DatabaseName { get; }
    }

    /// <summary>
    /// An exception to be converted to a JSON error result.
    /// </summary>
    public sealed class JsonException : Exception
    {
        private readonly int _statusCode;
        public JsonException(int statusCode, string message)
            : base(message)
        {
            _statusCode = statusCode;
        }
        public int StatusCode { get { return _statusCode; } }
    }

    /// <summary>
    /// Useful extension methods.
    /// </summary>
    public static class Extensions
    {
        public static T[] AsArrayOrEmpty<T>(this T item) where T : class
        {
            if (Object.ReferenceEquals(item, null)) return new T[0];
            return new T[1] { item };
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            return source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable),
                "Skip",
                new Type[] { source.ElementType },
                source.Expression,
                Expression.Constant(count)
            ));
        }

        public static IQueryable Take(this IQueryable source, int count)
        {
            return source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable),
                "Take",
                new Type[] { source.ElementType },
                source.Expression,
                Expression.Constant(count)
            ));
        }

#if false
        // NOTE(jsd): LINQ-to-SQL rejected my idea because "Explicit construction of entity type [type] in query is not allowed"
        public static IQueryable Select(this IQueryable source, Type destType, params string[] names)
        {
            // Create the parameter to the lambda:
            ParameterExpression prm = Expression.Parameter(source.ElementType, "_");
            // Create the projection expression to initialize the selected members:
            Expression projection = Expression.MemberInit(Expression.New(destType), (
                from name in names
                let srcProp = findPropertyByName(source.ElementType, name)
                let dstProp = findPropertyByName(destType, name)
                select (MemberBinding) Expression.Bind(dstProp, Expression.Property(prm, srcProp))
            ));

            return source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable),
                "Select",
                new Type[] { source.ElementType, source.ElementType },
                new Expression[] { source.Expression, Expression.Lambda(projection, prm) }
            ));
        }
#endif

        static IOrderedQueryable ordered(IQueryable source, string method, string name)
        {
            PropertyInfo prop = findPropertyByName(source.ElementType, name);
            ParameterExpression prm = Expression.Parameter(source.ElementType, "_");

            return (IOrderedQueryable)source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable),
                method,
                new Type[] { source.ElementType, prop.PropertyType },
                new Expression[] { source.Expression, Expression.Lambda(Expression.Property(prm, prop), prm) }
            ));
        }

        public static IOrderedQueryable OrderBy(this IQueryable source, string name)
        {
            return ordered(source, "OrderBy", name);
        }

        public static IOrderedQueryable OrderByDescending(this IQueryable source, string name)
        {
            return ordered(source, "OrderByDescending", name);
        }

        static IOrderedQueryable thenOrdered(IOrderedQueryable source, string method, string name)
        {
            PropertyInfo prop = findPropertyByName(source.ElementType, name);
            ParameterExpression prm = Expression.Parameter(source.ElementType, "_");

            return (IOrderedQueryable)source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable),
                method,
                new Type[] { source.ElementType, prop.PropertyType },
                new Expression[] { source.Expression, Expression.Lambda(Expression.Property(prm, prop), prm) }
            ));
        }

        public static IOrderedQueryable ThenBy(this IOrderedQueryable source, string name)
        {
            return thenOrdered(source, "ThenBy", name);
        }

        public static IOrderedQueryable ThenByDescending(this IOrderedQueryable source, string name)
        {
            return thenOrdered(source, "ThenByDescending", name);
        }

        public static IQueryable Where(this IQueryable source, Expression predicate)
        {
            return source.Provider.CreateQuery(Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] { source.ElementType },
                new Expression[] { source.Expression, Expression.Quote(predicate) }
            ));
        }

        public enum BinaryOperator
        {
            Equal,
            NotEqual,
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual,
            SqlLike
        }

        static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static PropertyInfo findPropertyByName(Type type, string propertyName)
        {
            // Find the Property by name on the query's element type:
            PropertyInfo prop;
            prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
                throw new ArgumentException(String.Format("Could not find property '{0}' on type '{1}'", propertyName, type.FullName), "propertyName");

            return prop;
        }

#if false
        static BinaryExpression lift(Func<Expression, Expression, BinaryExpression> compare, Expression l, Expression r)
        {
            // Lifts the non-nullable type to a nullable type:
            if (IsNullableType(l.Type) && !IsNullableType(r.Type))
                r = Expression.Convert(r, l.Type);
            else if (!IsNullableType(l.Type) && IsNullableType(r.Type))
                l = Expression.Convert(l, r.Type);

            return compare(l, r);
        }

        public static IQueryable FilterByComparison(this IQueryable source, BinaryOperator op, string propertyName, Func<Type, object> getComparand)
        {
            // Find the Property by name on the query's element type:
            PropertyInfo prop = findPropertyByName(source.ElementType, propertyName);

            // Parameter to the lambda for Where:
            ParameterExpression prm = Expression.Parameter(source.ElementType, "_");

            // Property access expression on the parameter passed to the lambda:
            Expression l = Expression.Property(prm, prop);
            // Function to get a comparand value from our caller with a specific type:
            Func<Type, Expression> r = (type) => Expression.Constant(getComparand(type));

            // Determine the type of comparison to make:
            Expression comparer;
            switch (op)
            {
                case BinaryOperator.Equal: comparer = lift(Expression.Equal, l, r(l.Type)); break;
                case BinaryOperator.NotEqual: comparer = lift(Expression.NotEqual, l, r(l.Type)); break;
                case BinaryOperator.LessThan: comparer = lift(Expression.LessThan, l, r(l.Type)); break;
                case BinaryOperator.LessThanOrEqual: comparer = lift(Expression.LessThanOrEqual, l, r(l.Type)); break;
                case BinaryOperator.GreaterThan: comparer = lift(Expression.GreaterThan, l, r(l.Type)); break;
                case BinaryOperator.GreaterThanOrEqual: comparer = lift(Expression.GreaterThanOrEqual, l, r(l.Type)); break;
                case BinaryOperator.SqlLike:
                    // Convert the `l` expression to a string using `.ToString()`:
                    if (l.Type != typeof(string)) l = Expression.Call(l, "ToString", Type.EmptyTypes);
                    // Call `System.Data.Linq.SqlClient.SqlMethods.Like` method:
                    comparer = Expression.Call(
                        // Find the specific `Like` static method:
                        typeof(System.Data.Linq.SqlClient.SqlMethods).GetMethod("Like", new Type[2] { typeof(string), typeof(string) }),
                        // Left, Right expressions are parameters to the static method:
                        l,
                        r(typeof(string))
                    );
                    break;
                default: throw new Exception(String.Format("Unhandled operator kind {0}", op.ToString()));
            }

            // Build the Where lambda:
            return source.Where(Expression.Lambda(comparer, prm));
        }
#endif

        ///////////////////////////////////////////////////////////////////

        static void lift(ref Expression l, ref Expression r)
        {
            // Lifts the non-nullable type to a nullable type:
            if (IsNullableType(l.Type) && !IsNullableType(r.Type))
                r = Expression.Convert(r, typeof(Nullable<>).MakeGenericType(r.Type));
            else if (!IsNullableType(l.Type) && IsNullableType(r.Type))
                l = Expression.Convert(l, typeof(Nullable<>).MakeGenericType(l.Type));
        }

        static Expression createOr(Expression l, Expression r)
        {
            return Expression.Or(l, r);
        }

        static Expression createAnd(Expression l, Expression r)
        {
            return Expression.And(l, r);
        }

        static void coerceTypes(ref Expression l, ref Expression r)
        {
            // Coerce expression types to a unified type for comparison:
            if (l.Type == r.Type) return;
            
            // Do we need to lift either to a Nullable<T>?
            lift(ref l, ref r);

            try
            {
                r = Expression.Convert(r, l.Type);
            }
            catch (InvalidOperationException)
            {
                // No coercion operator defined between the types.
                try
                {
                    l = Expression.Convert(l, r.Type);
                }
                catch (InvalidOperationException)
                {
                    // No coercion operator defined between the types.
                    
                    throw new Exception("I GIVE UP");
                }
            }
#if false
            if (l.Type == typeof(Guid) && r.Type == typeof(string))
                r = Expression.New(typeof(Guid).GetConstructor(new Type[] { typeof(String) }), r);
#endif
        }

        static Expression createEqual(string op, Expression l, Expression r)
        {
            coerceTypes(ref l, ref r);
            switch (op)
            {
                case "eq": return Expression.Equal(l, r);
                case "ne": return Expression.NotEqual(l, r);
                default: throw new NotSupportedException();
            }
        }

        static Expression createCompare(string op, Expression l, Expression r)
        {
            coerceTypes(ref l, ref r);
            switch (op)
            {
                case "lt": return Expression.LessThan(l, r);
                case "le": return Expression.LessThanOrEqual(l, r);
                case "gt": return Expression.GreaterThan(l, r);
                case "ge": return Expression.GreaterThanOrEqual(l, r);
                case "like":
                    // HACK(jsd): Convert the `l` and `r` expressions to strings using `.ToString()`:
                    if (l.Type != typeof(string)) l = Expression.Call(l, "ToString", Type.EmptyTypes);
                    if (r.Type != typeof(string)) r = Expression.Call(r, "ToString", Type.EmptyTypes);

                    // Call `System.Data.Linq.SqlClient.SqlMethods.Like` method:
                    return Expression.Call(
                        // Find the specific `Like` static method:
                        typeof(System.Data.Linq.SqlClient.SqlMethods).GetMethod("Like", new Type[2] { typeof(string), typeof(string) }),
                        // Left, Right expressions are parameters to the static method:
                        l,
                        r
                    );
                case "in":
                    // TODO(jsd): lift to nullable?
                    return Expression.Call(typeof(Enumerable), "Contains", new Type[] { l.Type, r.Type }, l, r);

                default: throw new NotSupportedException();
            }
        }

        public static IQueryable TranslateToExpression(this IQueryable source, ExpressionLibrary.Expression root)
        {
            Type elType = source.ElementType;

            // Parameter to the lambda for Where:
            ParameterExpression prm = Expression.Parameter(source.ElementType, "_");

            Func<ExpressionLibrary.Expression, Expression> visitor = null;
            visitor = (e) =>
            {
                ExpressionLibrary.BinaryExpression binExp;
                ExpressionLibrary.EqualExpression eqExp;
                ExpressionLibrary.CompareExpression cmpExp;

                switch (e.ExpressionKind)
                {
                    case ExpressionLibrary.Expression.Kind.Identifier:
                        // Identifiers are turned into property lookups on the lambda's parameter:
                        return Expression.Property(prm, findPropertyByName(elType, ((ExpressionLibrary.IdentifierExpression)e).Identifier.Value));
                    case ExpressionLibrary.Expression.Kind.Null:
                        // FIXME(jsd): add Type parameter?
                        return Expression.Constant(null);
                    case ExpressionLibrary.Expression.Kind.Boolean:
                        return Expression.Constant(((ExpressionLibrary.BooleanExpression)e).Value);
                    case ExpressionLibrary.Expression.Kind.String:
                        return Expression.Constant(((ExpressionLibrary.StringExpression)e).String.Value);
                    case ExpressionLibrary.Expression.Kind.Integer:
                        return Expression.Constant(Int64.Parse(((ExpressionLibrary.IntegerExpression)e).Integer.Value));
                    case ExpressionLibrary.Expression.Kind.Decimal:
                        return Expression.Constant(Decimal.Parse(((ExpressionLibrary.DecimalExpression)e).Decimal.Value));
                    case ExpressionLibrary.Expression.Kind.List:
                        // TODO(jsd): Unify the array element types.
                        return Expression.NewArrayInit(typeof(object), ((ExpressionLibrary.ListExpression)e).Elements.Select(el => el.Visit(visitor)));
                    case ExpressionLibrary.Expression.Kind.BinOr:
                        binExp = (ExpressionLibrary.BinaryExpression)e;
                        return createOr(binExp.Left.Visit(visitor), binExp.Right.Visit(visitor));
                    case ExpressionLibrary.Expression.Kind.BinAnd:
                        binExp = (ExpressionLibrary.BinaryExpression)e;
                        return createAnd(binExp.Left.Visit(visitor), binExp.Right.Visit(visitor));
                    case ExpressionLibrary.Expression.Kind.BinEqual:
                        binExp = eqExp = (ExpressionLibrary.EqualExpression)e;
                        return createEqual(eqExp.Token.Value, binExp.Left.Visit(visitor), binExp.Right.Visit(visitor));
                    case ExpressionLibrary.Expression.Kind.BinCmp:
                        binExp = cmpExp = (ExpressionLibrary.CompareExpression)e;
                        return createCompare(cmpExp.Token.Value, binExp.Left.Visit(visitor), binExp.Right.Visit(visitor));

                    default: throw new NotSupportedException();
                }
            };

            // Visit the filter expression tree to translate them into a LINQ Expression tree:
            Expression expr = root.Visit(visitor);

            // Build the Where lambda:
            return source.Where(Expression.Lambda(expr, prm));
        }
    }
}

namespace WellDunne.WebTools.ExpressionLibrary
{
    public enum TokenKind
    {
        Invalid,

        Identifier,
        True,
        False,
        Null,
        Operator,

        StringLiteral,
        IntegerLiteral,
        DecimalLiteral,

        Comma,
        ParenOpen,
        ParenClose,
        BracketOpen,
        BracketClose
    }

    [System.Diagnostics.DebuggerDisplay("{Kind} - {ToString()}")]
    public struct Token
    {
        private readonly TokenKind _kind;
        private readonly long _position;
        private readonly string _value;
        private readonly bool _isReservedWord;

        private static readonly HashSet<string> _reservedWords = new HashSet<string>(new string[] {
            "null", "true", "false",
            "eq", "ne", "lt", "gt", "le", "ge", "like", "in", "not", "and", "or"
        });

        public TokenKind Kind { get { return _kind; } }
        public long Position { get { return _position; } }
        public string Value { get { return _value; } }
        public bool IsReservedWord { get { return _isReservedWord; } }

        public Token(TokenKind kind, long position, string value)
        {
            _kind = kind;
            _position = position;
            _value = value;
            _isReservedWord = (kind == TokenKind.Identifier && _reservedWords.Contains(value));
        }

        public Token(TokenKind kind, long position)
        {
            _kind = kind;
            _position = position;
            _value = null;
            _isReservedWord = false;
        }

        public override string ToString()
        {
            switch (_kind)
            {
                case TokenKind.Invalid: return "<INVALID>";
                case TokenKind.Identifier: return identifier();
                case TokenKind.Operator: return _value;
                case TokenKind.IntegerLiteral: return _value;
                case TokenKind.DecimalLiteral: return _value;
                case TokenKind.Null: return "null";
                case TokenKind.True: return "true";
                case TokenKind.False: return "false";
                case TokenKind.ParenOpen: return "(";
                case TokenKind.ParenClose: return ")";
                case TokenKind.BracketOpen: return "[";
                case TokenKind.BracketClose: return "]";
                case TokenKind.Comma: return ",";
                case TokenKind.StringLiteral: return String.Concat("\'", escapeString(_value), "\'");
                default: return String.Format("<unknown token {0}>", _value ?? _kind.ToString());
            }
        }

        private string identifier()
        {
            if (_isReservedWord) return "@" + _value;
            return _value;
        }

        internal static string kindToString(TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.Invalid: return "<INVALID>";
                case TokenKind.Identifier: return "identifier";
                case TokenKind.Operator: return "operator";
                case TokenKind.IntegerLiteral: return "integer";
                case TokenKind.DecimalLiteral: return "decimal";
                case TokenKind.Null: return "'null'";
                case TokenKind.True: return "'true'";
                case TokenKind.False: return "'false'";
                case TokenKind.ParenOpen: return "'('";
                case TokenKind.ParenClose: return "')'";
                case TokenKind.BracketOpen: return "'['";
                case TokenKind.BracketClose: return "']'";
                case TokenKind.Comma: return "','";
                case TokenKind.StringLiteral: return "'string'";
                default: return String.Format("<unknown token kind {0}>", kind.ToString());
            }
        }

        internal static string escapeString(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\n') sb.Append("\\n");
                else if (ch == '\r') sb.Append("\\r");
                else if (ch == '\t') sb.Append("\\t");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch == '\'') sb.Append("\\\'");
                else if (ch == '\"') sb.Append("\\\"");
                else sb.Append(ch);
            }
            return sb.ToString();
        }
    }

    public sealed class Lexer
    {
        private readonly TextReader _reader;
        private long _charPosition;

        public Lexer(TextReader sr)
        {
            _reader = sr;
            _charPosition = 0;
        }

        public Lexer(TextReader sr, long charPosition)
        {
            _reader = sr;
            _charPosition = charPosition;
        }

        public IEnumerable<Token> Lex()
        {
            while (!EndOfStream())
            {
                // Peek at the current character:
                char c = Peek();
                if (Char.IsWhiteSpace(c))
                {
                    c = Read();
                    while (!EndOfStream() && Char.IsWhiteSpace(c = Peek()))
                    {
                        // Consume the whitespace:
                        Read();
                    }
                }

                // Record our current stream position:
                long position = Position();

                if (c == '_' || c == '@' || Char.IsLetter(c))
                {
                    // Start consuming an identifer:
                    var sb = new StringBuilder(8);

                    // Starting an identifier with '@' allows identifiers to be reserved words.
                    bool forceIdent = (c == '@');
                    if (c == '@') Read();

                    sb.Append(Read());
                    while (!EndOfStream())
                    {
                        char c2 = Peek();
                        if (c2 == '_' || Char.IsLetterOrDigit(c2))
                            sb.Append(Read());
                        else
                            break;
                    }

                    // Now determine what kind of token this identifier is:
                    string ident = sb.ToString();
                    if (forceIdent)
                        yield return new Token(TokenKind.Identifier, position, ident);
                    else if (ident == "null")
                        yield return new Token(TokenKind.Null, position, ident);
                    else if (ident == "true")
                        yield return new Token(TokenKind.True, position, ident);
                    else if (ident == "false")
                        yield return new Token(TokenKind.False, position, ident);
                    else if (operatorNames.Contains(ident))
                        yield return new Token(TokenKind.Operator, position, ident);
                    else
                        yield return new Token(TokenKind.Identifier, position, ident);
                }
                else if (Char.IsDigit(c) || c == '-')
                {
                    // Start consuming a numeric literal:
                    var sb = new StringBuilder(8);

                    bool isDecimal = false;
                    sb.Append(Read());

                    while (!EndOfStream())
                    {
                        char c2 = Peek();
                        if (isDecimal)
                        {
                            if (Char.IsDigit(c2))
                                sb.Append(Read());
                            else
                                break;
                        }
                        else
                        {
                            // HACK(jsd): Should create a new state to parse decimals properly.
                            if (c2 == '.')
                            {
                                isDecimal = true;
                                sb.Append(Read());
                            }
                            else if (Char.IsDigit(c2))
                                sb.Append(Read());
                            else
                                break;
                        }
                    }

                    if (isDecimal)
                        yield return new Token(TokenKind.DecimalLiteral, position, sb.ToString());
                    else
                        yield return new Token(TokenKind.IntegerLiteral, position, sb.ToString());
                }
                else if (c == '\'')
                {
                    bool error = false;

                    // Start consuming a quoted string:
                    var sb = new StringBuilder(32);
                    // Consume the opening quote char:
                    Read();

                    while (!error & !EndOfStream())
                    {
                        char c2 = Peek();
                        if (c2 == '\'')
                        {
                            // End of the string:
                            // Consume the '\'':
                            Read();
                            break;
                        }
                        else if (c2 == '\\')
                        {
                            // Escaped char:
                            // Consume the '\\':
                            Read();
                            // Consume the second char:
                            char c3;
                            switch (c3 = Read())
                            {
                                case '\\': sb.Append('\\'); break;
                                case '\'': sb.Append('\''); break;
                                case '\"': sb.Append('\"'); break;
                                case 'n': sb.Append('\n'); break;
                                case 'r': sb.Append('\r'); break;
                                case 't': sb.Append('\t'); break;
                                default:
                                    error = true;
                                    yield return new Token(TokenKind.Invalid, Position(), String.Format("Unrecognized escape sequence '\\{0}' at position {1}", c3, Position() + 1));
                                    break;
                            }
                        }
                        else
                        {
                            // Consume the char and add it to the string:
                            sb.Append(Read());
                        }
                    }

                    if (!error)
                        yield return new Token(TokenKind.StringLiteral, position, sb.ToString());
                }
                else if (c == ',')
                {
                    Read();
                    yield return new Token(TokenKind.Comma, position, c.ToString());
                }
                else if (c == '(')
                {
                    Read();
                    yield return new Token(TokenKind.ParenOpen, position, c.ToString());
                }
                else if (c == ')')
                {
                    Read();
                    yield return new Token(TokenKind.ParenClose, position, c.ToString());
                }
                else if (c == '[')
                {
                    Read();
                    yield return new Token(TokenKind.BracketOpen, position, c.ToString());
                }
                else if (c == ']')
                {
                    Read();
                    yield return new Token(TokenKind.BracketClose, position, c.ToString());
                }
                else
                {
                    Read();
                    yield return new Token(TokenKind.Invalid, position, String.Format("Unrecognized character '{0}' at position {1}", c, position + 1));
                }
            }

            yield break;
        }

        private char Read()
        {
            ++_charPosition;
            return (char)_reader.Read();
        }

        private char Peek() { return (char)_reader.Peek(); }
        private long Position() { return _charPosition; }
        private bool EndOfStream() { return _reader.Peek() == -1; }

        private static readonly HashSet<string> operatorNames = new HashSet<string>(new string[] {
            "eq", "ne", "lt", "gt", "le", "ge", "like", "in", "not", "and", "or"
        });
    }

    public abstract class Expression
    {
        public enum Kind
        {
            Identifier,
            String,
            Integer,
            Decimal,
            Null,
            Boolean,
            List,
            BinCmp,
            BinOr,
            BinAnd,
            BinEqual
        }

        private readonly Kind _kind;
        public Kind ExpressionKind { get { return _kind; } }

        protected Expression(Kind kind)
        {
            _kind = kind;
        }

        /// <summary>
        /// Writes the expression to the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="tw"></param>
        public virtual void WriteTo(TextWriter tw)
        {
            tw.Write("<expr>");
        }

        /// <summary>
        /// Formats the expression as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (var tw = new StringWriter())
            {
                WriteTo(tw);
                return tw.ToString();
            }
        }

        public T Visit<T>(Func<Expression, T> visitor)
        {
            return visitor(this);
        }
    }

    public class IdentifierExpression : Expression
    {
        private readonly Token _token;
        public Token Identifier { get { return _token; } }

        public IdentifierExpression(Token token)
            : base(Kind.Identifier)
        {
            _token = token;
        }

        public override void WriteTo(TextWriter tw)
        {
            if (_token.IsReservedWord) tw.Write('@');
            tw.Write(_token.Value);
        }
    }

    public sealed class StringExpression : Expression
    {
        private readonly Token _token;
        public Token String { get { return _token; } }

        public StringExpression(Token tok)
            : base(Kind.String)
        {
            this._token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token);
        }
    }

    public sealed class IntegerExpression : Expression
    {
        private readonly Token _token;
        public Token Integer { get { return _token; } }

        public IntegerExpression(Token tok)
            : base(Kind.Integer)
        {
            this._token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token.Value);
        }
    }

    public sealed class DecimalExpression : Expression
    {
        private readonly Token _token;
        public Token Decimal { get { return _token; } }

        public DecimalExpression(Token tok)
            : base(Kind.Decimal)
        {
            this._token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token.Value);
        }
    }

    public sealed class NullExpression : Expression
    {
        private readonly Token _token;

        public NullExpression(Token tok)
            : base(Kind.Null)
        {
            _token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("null");
        }
    }

    public sealed class BooleanExpression : Expression
    {
        private readonly Token _token;
        private readonly bool _value;
        public bool Value { get { return _value; } }

        public BooleanExpression(Token tok, bool value)
            : base(Kind.Boolean)
        {
            this._token = tok;
            this._value = value;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_value ? "true" : "false");
        }
    }

    public sealed class ListExpression : Expression
    {
        private readonly Token _token;
        private readonly List<Expression> _elements;
        public List<Expression> Elements { get { return _elements; } }

        public ListExpression(Token tok, List<Expression> elements)
            : base(Kind.List)
        {
            _token = tok;
            _elements = elements;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("[");
            for (int i = 0; i < _elements.Count; ++i)
            {
                _elements[i].WriteTo(tw);
                if (i < _elements.Count - 1) tw.Write(",");
            }
            tw.Write("]");
        }
    }

    public abstract class BinaryExpression : Expression
    {
        protected readonly Expression _l;
        protected readonly Expression _r;

        public Expression Left { get { return _l; } }
        public Expression Right { get { return _r; } }

        protected BinaryExpression(Kind kind, Expression l, Expression r)
            : base(kind)
        {
            _l = l;
            _r = r;
        }

        protected abstract void WriteInner(TextWriter tw);

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("(");
            Left.WriteTo(tw);
            WriteInner(tw);
            Right.WriteTo(tw);
            tw.Write(")");
        }
    }

    public class OrExpression : BinaryExpression
    {
        private readonly Token _token;

        public OrExpression(Token tok, Expression l, Expression r)
            : base(Kind.BinOr, l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" or ");
        }
    }

    public sealed class AndExpression : BinaryExpression
    {
        private readonly Token _token;

        public AndExpression(Token tok, Expression l, Expression r)
            : base(Kind.BinAnd, l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" and ");
        }
    }

    public sealed class CompareExpression : BinaryExpression
    {
        private readonly Token _token;
        public Token Token { get { return _token; } }

        public CompareExpression(Token tok, Expression l, Expression r)
            : base(Kind.BinCmp, l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" " + _token.Value + " ");
        }
    }

    public sealed class EqualExpression : BinaryExpression
    {
        private readonly Token _token;
        public Token Token { get { return _token; } }

        public EqualExpression(Token tok, Expression l, Expression r)
            : base(Kind.BinEqual, l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" " + _token.Value + " ");
        }
    }

    public sealed class ParserError
    {
        private readonly Token _token;
        private readonly string _message;

        public Token Token { get { return _token; } }
        public string Message { get { return _message; } }

        public ParserError(Token tok, string message)
        {
            _token = tok;
            _message = message;
        }
    }

    /// <summary>
    /// Main entry point to the expression parser.
    /// </summary>
    public sealed class Parser
    {
        private readonly Lexer _lexer;
        private IEnumerator<Token> _tokens;
        private bool _eof;
        private Token _lastToken;
        private List<ParserError> _errors;
        private int _position;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _tokens = _lexer.Lex().GetEnumerator();
            _errors = new List<ParserError>();
        }

        /// <summary>
        /// Parses the expression lexed by the lexer and returns a boolean that indicates success or failure.
        /// </summary>
        /// <param name="result">Resulting expression instance that represents the parsed expression tree or null if failed</param>
        /// <returns>true if succeeded in parsing, false otherwise</returns>
        public bool ParseExpression(out Expression result)
        {
            using (_tokens)
            {
                result = null;
                if (!AdvanceOrError("Expected expression")) return false;

                bool success = parseExpression(out result);
                if (!success) return false;

                if (!Eof())
                {
                    Error("Expected end of expression");
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the collection of parser errors generated during the last ParseExpression.
        /// </summary>
        /// <returns></returns>
        public List<ParserError> GetErrors()
        {
            return new List<ParserError>(_errors);
        }

        private bool parseExpression(out Expression e)
        {
            if (!parseOrExp(out e)) return false;
            return true;
        }

        private bool parseOrExp(out Expression e)
        {
            if (!parseAndExp(out e)) return false;

            Expression e2;
            Token tok;

            while (Current.Kind == TokenKind.Operator && Current.Value == "or")
            {
                tok = Current;
                if (!AdvanceOrError("Expected expression after 'or'")) return false;
                if (!parseAndExp(out e2)) return false;
                e = new OrExpression(tok, e, e2);
            }
            return true;
        }

        private bool parseAndExp(out Expression e)
        {
            if (!parseCmpExp(out e)) return false;

            Expression e2;
            Token tok;

            while (Current.Kind == TokenKind.Operator && Current.Value == "and")
            {
                tok = Current;
                if (!AdvanceOrError("Expected expression after 'and'")) return false;
                if (!parseCmpExp(out e2)) return false;
                e = new AndExpression(tok, e, e2);
            }
            return true;
        }

        private bool parseCmpExp(out Expression e)
        {
            if (!parseUnaryExp(out e)) return false;
            if (Current.Kind != TokenKind.Operator) return true;

            Expression e2;
            Token tok = Current;

            if (tok.Value == "eq" ||
                tok.Value == "ne")
            {
                if (!AdvanceOrError(String.Format("Expected expression after '{0}'", tok.Value))) return false;
                if (!parseUnaryExp(out e2)) return false;
                e = new EqualExpression(tok, e, e2);
            }
            else if
               (tok.Value == "lt" ||
                tok.Value == "le" ||
                tok.Value == "gt" ||
                tok.Value == "ge" ||
                tok.Value == "like" ||
                tok.Value == "in")
            {
                if (!AdvanceOrError(String.Format("Expected expression after '{0}'", tok.Value))) return false;
                if (!parseUnaryExp(out e2)) return false;
                e = new CompareExpression(tok, e, e2);
            }

            return true;
        }

        private bool parseUnaryExp(out Expression e)
        {
            return parsePrimaryExp(out e);
        }

        private bool parsePrimaryExp(out Expression e)
        {
            if (Current.Kind == TokenKind.Identifier)
                e = new IdentifierExpression(Current);
            else if (Current.Kind == TokenKind.Null)
                e = new NullExpression(Current);
            else if (Current.Kind == TokenKind.True)
                e = new BooleanExpression(Current, true);
            else if (Current.Kind == TokenKind.False)
                e = new BooleanExpression(Current, false);
            else if (Current.Kind == TokenKind.IntegerLiteral)
                e = new IntegerExpression(Current);
            else if (Current.Kind == TokenKind.DecimalLiteral)
                e = new DecimalExpression(Current);
            else if (Current.Kind == TokenKind.StringLiteral)
                e = new StringExpression(Current);
            else if (Current.Kind == TokenKind.BracketOpen)
            {
                e = null;
                Token tok = Current;
                if (!AdvanceOrError("Expected expression after '['")) return false;

                List<Expression> elements = new List<Expression>();
                while (Current.Kind != TokenKind.BracketClose & !Eof())
                {
                    Expression element;
                    if (!parseExpression(out element)) return false;

                    elements.Add(element);
                    if (Current.Kind == TokenKind.BracketClose) break;

                    // Expect a ',' after each expression:
                    if (!Check(TokenKind.Comma)) return false;
                    if (!AdvanceOrError("Expected expression after ','")) return false;
                }
                if (!Check(TokenKind.BracketClose)) return false;

                e = new ListExpression(tok, elements);
            }
            else if (Current.Kind == TokenKind.ParenOpen)
            {
                e = null;
                if (!AdvanceOrError("Expected expression after '('")) return false;
                if (!parseExpression(out e)) return false;
                if (!Check(TokenKind.ParenClose)) return false;
            }
            else
            {
                e = null;
                Error("Unexpected token '{0}'", Current);
                return false;
            }

            Advance();
            return true;
        }

        private Token Current { get { return _lastToken = _tokens.Current; } }
        private Token LastToken { get { return _lastToken; } }
        private int Position { get { return _position; } }

        private bool Eof() { return _eof; }
        private bool Advance()
        {
            if (_eof) return false;

            bool havenext = _tokens.MoveNext();
            if (!havenext) _eof = true;
            ++_position;
            return havenext;
        }

        private bool AdvanceOrError(string error)
        {
            bool havenext = Advance();
            if (!havenext) return Error(error);
            return havenext;
        }

        private bool Check(TokenKind tokenKind)
        {
            if (Eof())
                return Error("Unexpected end of expression while looking for {0}", Token.kindToString(tokenKind));
            if (Current.Kind != tokenKind)
                return Error("Expected {0} but found {1}", Token.kindToString(tokenKind), Current);
            return true;
        }

        private bool Error(string error)
        {
            _errors.Add(new ParserError(_lastToken, error));
            return false;
        }

        private bool Error(string errorFormat, params object[] args)
        {
            Error(String.Format(errorFormat, args));
            return false;
        }
    }
}

namespace WellDunne.WebTools
{
    /// <summary>
    /// Base class implementation for ISimpleDataProvider assuming a derived implementation uses LINQ-to-SQL.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DataProviderBase<T> : ISimpleDataProvider where T : class
    {
        // NOTE(jsd): In the derived class, these must be set in the ctor:

        /// <summary>
        /// Get an IQueryable&lt;<typeparamref name="T"/>&gt; to query over the data for the entity.
        /// </summary>
        protected Func<IQueryable<T>> m_Query;
        /// <summary>
        /// Get an instance of <typeparamref name="T"/> or null by its identity. Modifications made to
        /// the retrieved instance can be persisted via <seealso cref="m_Submit"/>.
        /// </summary>
        protected Func<int, T> m_GetByIntegerID;
        /// <summary>
        /// Get an instance of <typeparamref name="T"/> or null by its identity. Modifications made to
        /// the retrieved instance can be persisted via <seealso cref="m_Submit"/>.
        /// </summary>
        protected Func<string, T> m_GetByRawID;
        /// <summary>
        /// Mark an instance of <typeparamref name="T"/> for insertion.
        /// </summary>
        protected Action<T> m_Insert;
        /// <summary>
        /// Mark an instance of <typeparamref name="T"/> for deletion.
        /// </summary>
        protected Action<T> m_Delete;
        /// <summary>
        /// Submit any updates, inserts, or deletes.
        /// </summary>
        protected Action m_Submit;

        private static readonly JsonSerializer json = JsonSerializer.Create(new JsonSerializerSettings
        {
            // NOTE(jsd): We need to include null values for proper deserialization from POST body
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        public IQueryable Query() { return m_Query(); }

        public object GetByID(string id)
        {
            return getByID(id);
        }

        private T getByID(string id)
        {
            if (m_GetByIntegerID != null)
            {
                int idValue;
                if (!Int32.TryParse(id, out idValue))
                    throw new JsonException(400, "Invalid id value in route");

                return m_GetByIntegerID(idValue);
            }
            else if (m_GetByRawID != null)
            {
                return m_GetByRawID(id);
            }

            throw new JsonException(500, "BUG: No delegate defined to handle GetByID");
        }

        public ArrayList InsertList(System.IO.Stream inputStream, Encoding encoding)
        {
            T ent;

            using (inputStream)
            using (var tr = new System.IO.StreamReader(inputStream, encoding))
            using (var jreq = new JsonTextReader(tr))
            {
                if (jreq.Read() & jreq.TokenType == JsonToken.StartArray)
                {
                    var al = new ArrayList();
                    while (jreq.Read())
                    {
                        if (jreq.TokenType == JsonToken.EndArray) break;

                        ent = json.Deserialize<T>(jreq);
                        if (ent == null) return null;

                        m_Insert(ent);
                        al.Add(ent);
                    }
                    return al;
                }
                else
                {
                    ent = json.Deserialize<T>(jreq);
                    if (ent == null) return null;

                    m_Insert(ent);

                    var al = new ArrayList(1);
                    al.Add(ent);
                    return al;
                }
            }
        }

        public object UpdateByID(string id, System.IO.Stream inputStream, Encoding encoding)
        {
            // Fetch the existing record:
            T ent = getByID(id);
            if (ent == null) return null;

            // Deserialize the POST body JSON onto the existing record:
            using (inputStream)
            using (var tr = new System.IO.StreamReader(inputStream, encoding))
            using (var jreq = new JsonTextReader(tr))
                json.Populate(jreq, ent);

            return ent;
        }

        public object DeleteByID(string id)
        {
            // Fetch the existing record:
            T ent = getByID(id);
            if (ent == null) return null;

            // Mark it for deletion:
            m_Delete(ent);

            return ent;
        }

        public void UpdateList(ArrayList entities, System.IO.Stream inputStream, Encoding encoding)
        {
            // Deserialize the POST body JSON onto the existing set of records, updating by position:
            using (inputStream)
            using (var tr = new System.IO.StreamReader(inputStream, encoding))
            using (var jreq = new JsonTextReader(tr))
            {
                var en = entities.GetEnumerator();
                if (jreq.Read() && jreq.TokenType != JsonToken.StartArray)
                    throw new JsonException(400, String.Format("Expected start of JSON array in POST body at Line {0} Position {1}", jreq.LineNumber, jreq.LinePosition));
                // Extra records in the POST body are ignored.
                while (en.MoveNext() & jreq.Read())
                {
                    if (jreq.TokenType == JsonToken.EndArray) break;
                    var ent = en.Current;

                    // Populate the current JSON object onto the existing entity for update:
                    json.Populate(jreq, ent);
                }
            }
        }

        public void DeleteList(ArrayList entities)
        {
            // Mark all entities for deletion:
            foreach (object ent in entities)
                m_Delete((T)ent);
        }

        public void Submit()
        {
            // Submit any changes:
            m_Submit();
        }

        protected abstract System.Data.Linq.DataContext repository { get; }
        public string ServerName { get { if (repository == null) return null; else return ((System.Data.Common.DbConnection)repository.Connection).DataSource; } }
        public string DatabaseName { get { if (repository == null) return null; else return ((System.Data.Common.DbConnection)repository.Connection).Database; } }

        protected virtual IEnumerable<IDisposable> disposables { get { return Enumerable.Repeat((IDisposable)repository, 1); } }

        public void Dispose()
        {
            var set = disposables;
            if (set == null) return;
            foreach (IDisposable disposable in set)
            {
                try { if (disposable != null) disposable.Dispose(); }
                catch { }
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Register your ISimpleDataProvider implementations here.
    ///////////////////////////////////////////////////////////////////////////////////////////////////////

    public static class EntityRegistrar
    {
        /// <summary>
        /// This dictionary defines the list of entities and their data providers used by the tool.
        /// </summary>
        public static readonly IDictionary<string, Func<ISimpleDataProvider>> Entities =
            // NOTE(jsd): One can replace this static dictionary with an IDictionary implementation that
            // is more dynamic.
            new Dictionary<string, Func<ISimpleDataProvider>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Test", () => new TestDataProvider() },
            };
    }

    #region ISimpleDataProvider implementations

    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Define your ISimpleDataProvider implementations here.
    ///////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Test

    public sealed class TestRecord
    {
        public int ID { get; set; }
        public string Test1 { get; set; }
        public Guid Test2 { get; set; }
        public int Test3 { get; set; }
        public Int64 Test4 { get; set; }
        public DateTime Test5 { get; set; }
        public DateTimeOffset Test6 { get; set; }
        public bool Test7 { get; set; }
    }

    public sealed class TestDataProvider : DataProviderBase<TestRecord>
    {
        protected override IEnumerable<IDisposable> disposables { get { return null; } }
        protected override System.Data.Linq.DataContext repository { get { return null; } }

        public TestDataProvider()
        {
            // NOTE(jsd): In a real implementation, this would likely use a DataContext implementation, e.g. LINQ-to-SQL or -to-entities
            var testData = new List<TestRecord>
            {
                new TestRecord { ID = 1, Test1 = "test 1!", Test2 = Guid.NewGuid(), Test3 = 11, Test4 = Int64.MaxValue, Test5 = DateTime.UtcNow, Test6 = DateTimeOffset.Now, Test7 = true },
                new TestRecord { ID = 2, Test1 = "test 2!", Test2 = Guid.NewGuid(), Test3 = 22, Test4 = Int64.MinValue, Test5 = DateTime.UtcNow, Test6 = DateTimeOffset.Now, Test7 = false },
                new TestRecord { ID = 3, Test1 = "test 3!", Test2 = Guid.Empty, Test3 = 33, Test4 = Int64.MinValue, Test5 = DateTime.UtcNow, Test6 = DateTimeOffset.Now, Test7 = false },
            };
            var pendingData = new List<TestRecord>(testData);

            // This returns a basic IQueryable used for filtering.
            m_Query = () => testData.AsQueryable();
            // This finds the record given an integer id:
            m_GetByIntegerID = (int id) => testData.SingleOrDefault(e => e.ID == id);
            // This finds the record given the raw id string parsed from the route; used for complex key scenarios:
            //m_GetByRawID = (string id) => { ... };
            // Marks a record for insertion upon next m_Submit() call:
            m_Insert = (ent) => pendingData.Add(ent);
            // Marks a record for deletion upon next m_Submit() call:
            m_Delete = (ent) => pendingData.Remove(ent);
            // Applies any entity inserts, updates, and deletes:
            m_Submit = () => { testData = pendingData; pendingData = new List<TestRecord>(testData); };
        }
    }

    #endregion

    #endregion
}