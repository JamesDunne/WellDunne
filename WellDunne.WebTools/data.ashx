<%@ WebHandler Language="C#" Class="WellDunne.WebTools.DataServiceProvider" %>
<%@ Assembly Name="System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="System.Data.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="WellDunne" %>

// Uncomment to use HTTP Basic authentication
//#define UseBasicAuth
// Allow JSONP requests
#define JSONP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace WellDunne.WebTools
{
    /// <summary>
    /// Main IHttpHandler for the tool.
    /// </summary>
    public class DataServiceProvider : IHttpHandler
    {
#if UseBasicAuth
        private const string httpBasicAuthUsername = "admin";
        private const string httpBasicAuthPassword = "admin";
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
      * $skip=n                         - Skip N rows
      * $top=n                          - Take top N rows
      * $filter=(name) (op) (value)     - Filter rows via simple comparison operations
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
            var jrsp = new Newtonsoft.Json.JsonTextWriter(rsp.Output);

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
#if UseBasicAuth
                string auth = req.Headers["Authorization"];
                if (auth == null) return new JsonResult(401, "Unauthorized");
                if (!auth.StartsWith("Basic ")) return new JsonResult(401, "Unauthorized");
                string b64up = auth.Substring(6);
                if (b64up != Convert.ToBase64String(Encoding.ASCII.GetBytes(httpBasicAuthUsername + ":" + httpBasicAuthPassword))) return new JsonResult(401, "Unauthorized");
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

        private static JsonResult getEntityByID(ISimpleDataProvider db, HttpRequest req, string id)
        {
            object ent = db.GetByID(id);
            if (ent == null) return new JsonResult(404, "Could not find record by id");

            return new JsonResult((object)ent.AsArrayOrEmpty());
        }

        private static Either<JsonResult, IQueryable> buildQuery(ISimpleDataProvider db, HttpRequest req)
        {
            // TODO(jsd): Add more querying support on `IQueryable`.
            IQueryable query = db.Query();
            int take = 1000;

            // Process query-string filters in order: (e.g. $skip before $top)
            var q = req.QueryString;
            for (int i = 0; i < q.Count; ++i)
            {
                string qkey = q.Keys[i];
                // NOTE(jsd): Unfortunately, ASP.NET unifies duplicate keys into a single key and gives the values in order without respect
                // to keys that might come in between duplicates.
                foreach (string qvalue in q.GetValues(i))
                {
                    // All filters start with '$':
                    if (qkey == null) continue;
                    if (qkey[0] != '$') continue;

                    string filter = qkey.Substring(1);

                    // Apply the filter operator:
                    if (filter.CaseInsensitiveTrimmedEquals("filter"))
                    {
                        // TODO(jsd): This regex works only for simple `a op b` binary expression comparisons.
                        var match = System.Text.RegularExpressions.Regex.Match(
                            qvalue,
                            @"^([_a-zA-Z][_a-zA-Z0-9]*)\s*(eq|ne|lt|le|gt|ge|like)\s*('.*'|[0-9]+(?:\.?[0-9]+)?)$"
                        );
                        if (!match.Success) return new JsonResult(400, String.Format("Bad filter expression `{0}`", qvalue));

                        // NOTE(jsd): Apparently Groups[0] is the entire match so the capture groups start at [1].
                        string propertyName = match.Groups[1].Value;
                        string op = match.Groups[2].Value;
                        string comparand = match.Groups[3].Value;

                        // Unescape the string:
                        if (comparand[0] == '\'')
                        {
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

        private static JsonResult getEntities(ISimpleDataProvider db, HttpRequest req)
        {
            return buildQuery(db, req).Collapse(
                jr => jr,
                // Must complete execution here for exception handling purposes:
                query => new JsonResult((object)query.Cast<object>().ToList())
            );
        }

        private static JsonResult updateByID(ISimpleDataProvider db, HttpRequest req, string id)
        {
            // UPDATE:
            var ent = db.UpdateByID(id, req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            if (ent == null) return new JsonResult(404, "Record to be updated could not be found by id");

            object meta;
            if (doCommit(req))
            {
                db.Submit();
                meta = new { committed = true };
            }
            else
            {
                meta = new { committed = false };
            }

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

                    object meta;
                    if (doCommit(req))
                    {
                        db.Submit();
                        meta = new { committed = true };
                    }
                    else
                    {
                        meta = new { committed = false };
                    }

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

                    object meta;
                    if (doCommit(req))
                    {
                        db.Submit();
                        meta = new { committed = true };
                    }
                    else
                    {
                        meta = new { committed = false };
                    }

                    return new JsonResult((object)deleted, (object)meta);
                }
            );
        }

        private static JsonResult insertEntities(ISimpleDataProvider db, HttpRequest req)
        {
            // INSERT:
            ArrayList newents = db.InsertList(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            if (newents == null) return new JsonResult(400, "No data provided to insert");

            object meta;
            if (doCommit(req))
            {
                db.Submit();
                meta = new { committed = true };
            }
            else
            {
                meta = new { committed = false };
            }

            return new JsonResult((object)newents, (object)meta);
        }

        private static JsonResult deleteByID(ISimpleDataProvider db, HttpRequest req, string id)
        {
            object ent = db.DeleteByID(id);
            if (ent == null) return new JsonResult(404, "Record to be deleted could not be found by id");

            object meta;
            if (doCommit(req))
            {
                db.Submit();
                meta = new { committed = true };
            }
            else
            {
                meta = new { committed = false };
            }

            return new JsonResult((object)ent.AsArrayOrEmpty(), (object)meta);
        }

        #region Utility and formatting methods

        private struct JsonResult
        {
            [Newtonsoft.Json.JsonIgnore]
            public readonly int statusCode;

            // NOTE(jsd): Fields are serialized to JSON in lexical definition order.
            public readonly bool success;
            public readonly string message;
            public readonly object errors;
            public readonly object meta;

            // NOTE(jsd): `results` must be last.
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

        // Ignore null values in JSON output:
        private static readonly Newtonsoft.Json.JsonSerializer json = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
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
            Newtonsoft.Json.JsonSerializationException jsex;
            System.Data.SqlClient.SqlException sqex;

            object innerException = null;
            if (ex.InnerException != null)
                innerException = (object)formatException(ex.InnerException);

            if ((jex = ex as JsonException) != null)
            {
                return new JsonResult(jex.StatusCode, jex.Message);
            }
            else if ((jsex = ex as Newtonsoft.Json.JsonSerializationException) != null)
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

        public static BinaryExpression lift(Func<Expression, Expression, BinaryExpression> compare, Expression l, Expression r)
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
            PropertyInfo prop;
            prop = source.ElementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                prop = source.ElementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
                throw new ArgumentException(String.Format("Could not find property '{0}' on type '{1}'", propertyName, source.ElementType.FullName), "propertyName");

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
    }

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

        // Ignore null values in JSON output:
        private static readonly Newtonsoft.Json.JsonSerializer json = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
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
            using (var jreq = new Newtonsoft.Json.JsonTextReader(tr))
            {
                if (jreq.Read() & jreq.TokenType == Newtonsoft.Json.JsonToken.StartArray)
                {
                    var al = new ArrayList();
                    while (jreq.Read())
                    {
                        if (jreq.TokenType == Newtonsoft.Json.JsonToken.EndArray) break;

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
            using (var jreq = new Newtonsoft.Json.JsonTextReader(tr))
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
            using (var jreq = new Newtonsoft.Json.JsonTextReader(tr))
            {
                var en = entities.GetEnumerator();
                if (jreq.Read() && jreq.TokenType != Newtonsoft.Json.JsonToken.StartArray)
                    throw new JsonException(400, String.Format("Expected start of JSON array in POST body at Line {0} Position {1}", jreq.LineNumber, jreq.LinePosition));
                // Extra records in the POST body are ignored.
                while (en.MoveNext() & jreq.Read())
                {
                    if (jreq.TokenType == Newtonsoft.Json.JsonToken.EndArray) break;
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

        protected abstract IDisposable repository { get; }
        public void Dispose() { try { if (repository != null) repository.Dispose(); } catch { } }
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
        public long Test4 { get; set; }
        public DateTime Test5 { get; set; }
        public DateTimeOffset Test6 { get; set; }
        public bool Test7 { get; set; }
    }

    public sealed class TestDataProvider : DataProviderBase<TestRecord>
    {
        private readonly IDisposable db;
        protected override IDisposable repository { get { return db; } }

        public TestDataProvider()
        {
            // NOTE(jsd): In a real implementation, this would likely use a DataContext implementation, e.g. LINQ-to-SQL or -to-entities
            db = null;
            var testData = new List<TestRecord>
            {
                new TestRecord { ID = 1, Test1 = "test 1!", Test2 = Guid.NewGuid(), Test3 = 11, Test4 = long.MaxValue, Test5 = DateTime.UtcNow, Test6 = DateTimeOffset.Now, Test7 = true },
                new TestRecord { ID = 2, Test1 = "test 2!", Test2 = Guid.NewGuid(), Test3 = 22, Test4 = long.MinValue, Test5 = DateTime.UtcNow, Test6 = DateTimeOffset.Now, Test7 = false },
                new TestRecord { ID = 3, Test1 = "test 3!", Test2 = Guid.Empty, Test3 = 33, Test4 = long.MinValue, Test5 = DateTime.UtcNow, Test6 = DateTimeOffset.Now, Test7 = false },
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