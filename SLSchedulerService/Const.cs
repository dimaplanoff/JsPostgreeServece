using SLLibrary;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SLScheduler
{
    public class ConstBase
    {
        public class TaskItem : BackgroundService
        {
            public string Name { get; set; }
            public string MailTo { get; set; }
            public string SqlScript { get; set; }
            public int Interval { get; set; }
            public DateTime? LastTime { get; set; }
            public bool MailOnSuccess { get; set; }
            public Dictionary<string, dynamic>[] Parameters { get; set; }


            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    if (isFree(now))
                        WorkItem(now);
                    GC.Collect();
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            private bool isFree(DateTime now) => !LastTime.HasValue || (now - LastTime.Value).TotalMinutes >= Interval;

            private void WorkItem(DateTime now)
            {
                StringBuilder mailMessage = new();
                try
                {
                    object result;

                    using (var context = new SLLibrary.SQL.Context(Const.Data.DB))
                    {
                        if (Parameters?.Length > 0)
                        {
                            List<object> args = new();
                            string authParams = string.Empty;
                            foreach (var p in Parameters)
                            {
                                var pName = p.First().Key;
                                if (!pName.StartsWith("p_"))
                                    pName = $"p_{p.First().Key}";
                                var pVal = p.First().Value;
                                authParams += $",{pName}";
                                if (pVal is string && ((string)pVal).StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {   
                                    var pUrl = ((string)pVal).Split(" ").FirstOrDefault(m=>m.StartsWith("http"));
                                    Dictionary<string, object> query = null;

                                    if (((string)pVal).Length > pUrl.Trim().Length)
                                    {
                                        var q = ((string)pVal).Substring(pUrl.Length).Trim();
                                        var o = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(q.Replace("'", "\""));
                                        if (o?.Count > 0)
                                        {
                                            query = new();

                                            foreach (var kv in o)
                                            {
                                                var n = kv.Value.ToString();
                                                if (n.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    var sParam = Regex.Match(n, @".+\(?<e>.*\)").Groups["e"].Value;
                                                    var sName = GetFuncName(n);
                                                    var sDataSrc = $@"select ""{sName}""({sParam})";
                                                    object sData;
                                                    if (kv.Key.Contains("json", StringComparison.OrdinalIgnoreCase) || kv.Key.Contains("result", StringComparison.OrdinalIgnoreCase))
                                                        sData = context.Execute<JsonElement>(null, sDataSrc);
                                                    else
                                                        sData = context.Execute<object>(null, sDataSrc);
                                               
                                                    query.Add(kv.Key, sData);
                                               
                                                }
                                                else if(n.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    object hData;
                                                    if (kv.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
                                                        hData = SLLibrary.WebClient.Instance.GetResponse<JsonElement>(pUrl, null, HttpMethod.Get).Data;
                                                    else
                                                        hData = SLLibrary.WebClient.Instance.GetResponse<object>(pUrl, null, HttpMethod.Get).Data;
                                               
                                                    query.Add(kv.Key, hData);
                                                }
                                                else
                                                    query.Add(kv.Key, kv.Value);
                                            }
                                        }
                                    }


                                    if (pName.Contains("json", StringComparison.OrdinalIgnoreCase))
                                        pVal = SLLibrary.WebClient.Instance.GetResponse<JsonElement>(pUrl, query, query == null ? HttpMethod.Get : HttpMethod.Post).Data;
                                    else
                                        pVal = SLLibrary.WebClient.Instance.GetResponse<object>(pUrl, query, query == null ? HttpMethod.Get : HttpMethod.Post).Data;                                
                                }
                               
                                args.Add(pVal);
                            }

                            if (authParams.Length > 0)
                                authParams = authParams.Substring(1);

                            var sqlScript = GetFuncName(SqlScript);
                            result = context.Execute<object>(null, $@"select ""{sqlScript}""({authParams})", args);
                        }
                        else
                            result = context.Execute<object>(null, SqlScript);

                        if (result == null)
                            mailMessage.Append("Procedure return null result");
                        else
                            mailMessage.AppendLine(JsonSerializer.Serialize(result, Const.JsonSerializerWeb));
                    }

                    if (MailOnSuccess)
                        Mail.Send(Const.Data.MailConf, $"Scheduler: {Name}", MailTo, mailMessage.ToString());

                }
                catch (Exception e)
                {
                    mailMessage.AppendLine(e.Message);
                    mailMessage.AppendLine(e.StackTrace);
                    while (e.InnerException != null)
                    {
                        e = e.InnerException;
                        mailMessage.AppendLine(e.Message);
                        mailMessage.AppendLine(e.StackTrace);
                    }

                    Mail.Send(Const.Data.MailConf, $"Scheduler ERROR: {Name}", MailTo, mailMessage.ToString());
                }
                finally
                {
                    LastTime = DateTime.Now;
                }
            }

        }

        public TaskItem[] Tasks { get; set; }
        public Mail.Config MailConf { get; set; }
        public string DB { get; set; }

        private static string GetFuncName(string cmd) => 
            cmd.Split(new[] { "select", "call", "select".ToUpper(), "call".ToUpper(), " ", "'", "\"", "(", ")" }, 
                StringSplitOptions.RemoveEmptyEntries).
            OrderByDescending(m => m.Length).First().Trim();


    }

    public class Const
    {

        public static ConstBase Data { get; private set; }
        public static readonly JsonSerializerOptions JsonSerializerWeb = LibHelper.JsonSerializerWeb;
        //public static readonly JsonSerializerOptions JsonSerializerWeb = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        //{
        //    WriteIndented = true,
        //    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        //};

        public static void Init(IConfiguration conf)
        {
            if (Data == null)
                Data = conf.Get<ConstBase>();
        }

    }
}
