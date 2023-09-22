using System.Text;
using System.Text.Json;


namespace UniApp
{
    public class Model
    {
        public class CPost 
        {
            public string Name { get; set; }
            public JsonElement? Value { get; set; }

            public object Write(Encoding encoding = null)
            {
                List<object> args = new();
                string authParams = string.Empty;

                if (Value != null && Value?.ValueKind != JsonValueKind.Null)
                {
                    var pName = $"@p_json";
                    authParams += $",{pName}";
                    args.Add(Value);
                }

                if (authParams.Length > 0)
                    authParams = authParams.Substring(1);

                using (SLLibrary.SQL.Context context = new(Const.J.DbConnectionString))
                    return context.Execute<JsonElement>(null, $@"select ""{Name}""({authParams})", args);
            }
        }
    }
}
