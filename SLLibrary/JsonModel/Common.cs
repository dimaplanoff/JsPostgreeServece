using System.Text.Json;

namespace SLLibrary.JsonModel
{
    public class Common
    {

        public class StandartResult
        {
            public class MetaInf
            {
                public int? page { get; set; }
                public int? pageTotal { get; set; }
                public int? perPage { get; set; }
                public int? rowPage { get; set; }
                public int? rowTotal { get; set; }
            }

            public int status { get; set; }
            public string message { get; set; }
            public MetaInf meta { get; set; }
            public object data { get; set; }

            public T Data<T>() => data == null ? default : JsonSerializer.Deserialize<T>(data is JsonElement ? ((JsonElement)data).GetRawText() : data.ToString(), LibHelper.JsonSerializerWeb);
        }

        public abstract class SqlTemplate
        {

            public virtual StandartResult Write(string procedureName, Identity.UserAuth user, string connStr)
            {
                return Write<StandartResult>(procedureName, user, connStr);
            }


            public virtual T Write<T>(string procedureName, Identity.UserAuth user, string connStr)
            {
                bool isStandartResult = typeof(T) == typeof(StandartResult);

                try
                {
                    List<object> args = new();
                    string authParams = string.Empty;
                    foreach (var p in GetType().GetProperties())
                    {
                        var pName = $"@p_{p.Name.ToLower()}";
                        var pVal = p.GetValue(this);
                        authParams += $",{pName}";
                        args.Add(pVal);
                    }

                    if (authParams.Length > 0)
                        authParams = authParams.Substring(1);

                    using (SQL.Context context = new(connStr))
                    {
                        if (isStandartResult)
                        {
                            var st = context.Execute<StandartResult>(user, $@"select ""{procedureName}""({authParams})", args);
                            if (st?.status == 1)
                                return (T)(object)st;
                            else
                                throw new Exception(st?.message ?? "Не удалось выполнить запрос");
                        }

                        return context.Execute<T>(user, $@"select ""{procedureName}""({authParams})", args);
                    }
                }
                catch (Exception e)
                {
                    if (isStandartResult)
                        return (T)(object)new StandartResult { status = -1, message = e.Message };
                    else
                        throw;
                }
            }
        }

        public class Page : SqlTemplate
        {
            public int? page { get; set; }
        }

        public class Empty : SqlTemplate
        {

        }                

        public class JsonObj : SqlTemplate
        {
            public JsonObj(string jsonCode)
            {
                var kv = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonCode, LibHelper.JsonSerializerWeb);
                json = kv.FirstOrDefault().Value;
            }
            
            public JsonElement json { get; private set; }
        }

    }
}
