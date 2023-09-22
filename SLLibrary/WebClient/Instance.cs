using System.Text.Json;
using System.Text;
using System.Net;

namespace SLLibrary.WebClient
{
    public enum Status
    {
        success = 1,
        error = 0
    }

    public class Answer<T>
    {
        public Status status { get; private set; }
        public string Message { get; private set; }
        public T Data { get; private set; }

        public Answer(Exception e)
        {
            status = Status.error;
            Message = e.Message;
        }

        public Answer(T dada)
        {
            status = Status.success;
            Data = dada;
        }

        public Answer()
        {
            status = Status.success;
        }
    }

    public class Instance
    {
        private static TimeSpan timeout => TimeSpan.FromSeconds(10);



        public static Answer<object> GetResponse(string url, object query, HttpMethod method = null, string content_type = null, params object[] headers)
        {
            return GetResponse<object>(url, query, method, content_type, headers);
        }


        public static async Task<Answer<object>> GetResponseAsync(string url, object query, HttpMethod method = null, string content_type = null, params object[] headers)
        {
            return await GetResponseAsync<object>(url, query, method, content_type, headers);
        }


        public static Answer<T> GetResponse<T>(string url, object query, HttpMethod method = null, string content_type = null, params object[] headers)
        {
            try
            {
                var tsk = GetResponseAsync<T>(url, query, method, content_type, headers);
                tsk.Wait(timeout);
                return tsk.Result;
            }
            catch (Exception e)
            {
                return new Answer<T>(e);
            }
        }        


        public static async Task<Answer<T>> GetResponseAsync<T>(string url, object query, HttpMethod method = null, string content_type = null, params object[] headers)
        {
            try
            {
                url = string.Join("://", url.Split("://", StringSplitOptions.RemoveEmptyEntries).Select(m=>m.Replace("//", "/")));

                using (CancellationTokenSource cancellation = new())
                {
                    cancellation.CancelAfter(timeout);

                    using (HttpClientHandler handler = new())
                    {
                        handler.UseCookies = false;
                        handler.AllowAutoRedirect = false;
                        handler.ServerCertificateCustomValidationCallback = (request, cert, chain, policy) => true;

                        HttpRequestMessage request = new HttpRequestMessage();

                        if (query == null && method == null)
                        {
                            request.Method = HttpMethod.Get;
                            request.RequestUri = new Uri(url);
                        }
                        else if (method == HttpMethod.Get)
                        {
                            if (query != null)
                            {
                                Type type = query.GetType();
                                var props = type.GetProperties();
                                string dataStr = string.Empty;
                                foreach (var prop in props)
                                {
                                    var val = prop.GetValue(query);
                                    if (val == null)
                                        continue;

                                    dataStr += $"&{prop.Name}={val}";
                                }

                                url += '?' + dataStr.Substring(1);
                            }
                            request.Method = HttpMethod.Get;
                            request.RequestUri = new Uri(url);
                        }
                        else
                        {                            
                            string bodyStr = query is string ? (string)query : JsonSerializer.Serialize(query);
                            HttpContent content = new StringContent(bodyStr, Encoding.UTF8, content_type ?? "application/json");
                            request.Method = method ?? HttpMethod.Post;
                            request.Content = content;
                            request.RequestUri = new Uri(url);
                        }


                        if (headers != null)
                        {
                            var headerQuery = headers.GetEnumerator();
                            string key, value;
                            while (headerQuery.MoveNext())
                            {
                                key = headerQuery.Current.ToString();
                                if (headerQuery.MoveNext())                                
                                { 
                                    value = headerQuery.Current.ToString();

                                    if (!string.IsNullOrEmpty(value))                                    
                                        request.Headers.Add(key, value);                                    
                                }
                            }
                        }


                        using (HttpClient httpClient = new HttpClient(handler))
                        {
                            using (HttpResponseMessage message = await httpClient.SendAsync(request, cancellation.Token))
                            {
                                if (message.StatusCode == HttpStatusCode.NoContent)
                                    return new Answer<T>();

                                string result = message.Content.ReadAsStringAsync().Result;

                                if (string.IsNullOrEmpty(result))
                                    return new Answer<T>();
                                
                                if (!new [] { HttpStatusCode.OK, HttpStatusCode.Created }.Contains( message.StatusCode))
                                    throw new Exception(JsonSerializer.Serialize(new { Status = "Error", Code = message.StatusCode, Meessage = result }));

                                //if(typeof(T) == typeof(object))
                                //    return new Answer<T>((T)(object)result);

                                if (typeof(T) == typeof(string))
                                        return new Answer<T>((T)(object)result);

                                return new Answer<T>(JsonSerializer.Deserialize<T>(result));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return new Answer<T>(e);
            }
        }
    }
}
