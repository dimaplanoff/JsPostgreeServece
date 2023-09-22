using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace SLLibrary.SQL
{
    public class Context : IDisposable 
    {       
        private string connstr;

        

        public Context(string connStr)
        {
            connstr = connStr;
        }


        public void Dispose()
        {
            GC.Collect();
        }

        private NpgsqlDbType TypeDetect(object e)
        {
            if (e == null)
                return NpgsqlDbType.Unknown;

            var type = e.GetType();
            if (type == typeof(string))
                return NpgsqlDbType.Text;
            if (type == typeof(int))
                return NpgsqlDbType.Integer;
            if (type == typeof(long))
                return NpgsqlDbType.Bigint;
            if (type == typeof(decimal))
                return NpgsqlDbType.Numeric;
            if (type == typeof(float))
                return NpgsqlDbType.Numeric;
            if (type == typeof(DateTime))
                return NpgsqlDbType.TimestampTz;
            if (type == typeof(bool))
                return NpgsqlDbType.Boolean;
            if (type == typeof(JsonElement))
                return NpgsqlDbType.Text;//Json;            

            return NpgsqlDbType.Unknown;
        }

        private object FixType(object obj, NpgsqlDbType type)
        {
            if (obj == null)
                return DBNull.Value;
            else if (type == NpgsqlDbType.TimestampTz)
                return ((DateTime)obj).ToUniversalTime();
            else if (type == NpgsqlDbType.Text && obj is JsonElement)
                return ((JsonElement)obj).GetRawText();
            return obj;
        }

        public void Execute(Identity.UserAuth user, string text, params object[] args)
        {
            Execute<object>(user, text, args);
        }


        public async void ExecuteAsync(Identity.UserAuth user, string text, params object[] args)
        {
            await ExecuteAsync<object>(user, text, args);
        }

        public T Execute<T>(Identity.UserAuth user, string text, params object[] args)
        {
            var tsk = ExecuteAsync<T>(user, text, args);
            tsk.Wait();
            return tsk.Result;
        }   
        

        public async Task<T> ExecuteAsync<T>(Identity.UserAuth user, string text, params object[] args)
        {

            using (var conn = new NpgsqlConnection(connstr))
            {

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                try
                {

                    if (user != null) //авторизация прописывается в переменную сессии sql
                    {
                        using (var authCmd = conn.CreateCommand())
                        {
                            string authParams = string.Empty;
                            foreach (var p in user.GetType().GetProperties())
                            {
                                var pVal = p.GetValue(user);
                                if (pVal == null) continue;
                                authParams += $",p_{p.Name}=>@p_{p.Name}";
                                authCmd.Parameters.AddWithValue($"@p_{p.Name}", TypeDetect(pVal), pVal);
                            }
                            if (authCmd.Parameters?.Count > 0)
                            {
                                authCmd.CommandText = $@"select ""fnSetContextClient""({authParams.Substring(1)});";

                                var authReader = await authCmd.ExecuteReaderAsync();
                                try
                                {
                                    while (await authReader.ReadAsync())
                                    {
                                        var authResult = JsonSerializer.Deserialize<JsonModel.Common.StandartResult>(authReader.GetValue(0)?.ToString(), LibHelper.JsonSerializerWeb);
                                        if (authResult?.status != 1)
                                            throw new Exception("Can not set session var");
                                        break;
                                    }
                                }
                                finally
                                {
                                    await authReader?.CloseAsync();
                                }
                            }
                        }
                    }

                    using (var cmd = (NpgsqlCommand)conn.CreateCommand())
                    {                        

                        if (args?.Length > 0)
                        {
                            List<object> targerArgs = args.Length == 1 && args[0] is List<object> ? (List<object>)args[0] : args.ToList();

                            cmd.CommandText = NamedCommand(text, out string[] parameters);
                            if (parameters.Length != targerArgs.Count)
                                throw new Exception("Wrong arguments length");

                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (targerArgs[i] == null)
                                    cmd.Parameters.AddWithValue(parameters[i], DBNull.Value);
                                else
                                {
                                    var npType = TypeDetect(targerArgs[i]);
                                    var npData = FixType(targerArgs[i], npType);
                                    cmd.Parameters.AddWithValue(parameters[i], npType, npData);
                                }
                            }
                        }
                        else
                            cmd.CommandText = text.Replace("'", "\"");


                        if (text.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        {

                            var reader = await cmd.ExecuteReaderAsync();
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var res = reader.GetValue(0);
                                    if (typeof(T) == typeof(JsonModel.Common.StandartResult))
                                        return JsonSerializer.Deserialize<T>(res.ToString(), LibHelper.JsonSerializerWeb);
                                    if (typeof(T) == typeof(JsonElement))                                    
                                        return (T)JsonSerializer.Deserialize<object>(res.ToString(), LibHelper.JsonSerializerWeb);                                    
                                    return (T)res;
                                }
                            }
                            finally
                            {
                                await reader?.CloseAsync();
                            }
                        }
                        else
                            await cmd.ExecuteNonQueryAsync();
                    }
                }
                finally
                {                         
                    conn?.Close();
                }

            }
            return default;
        }

        private static string NamedCommand(string cmd, out string[] commandParameters)
        {
            var regParts = Regex.Match(cmd.Trim(), @"(?<type>select|call)(\ )+(?<cmd>.+)\((?<params>[a-z0-9'\@\ \,_\-]+)*\).*", RegexOptions.IgnoreCase).Groups;
            var type = regParts["type"];
            var cmdtext = regParts["cmd"];
            var parameters = regParts["params"].Value.Split(new[] { ',', '@', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            commandParameters = parameters.Select(m => $"@{m}").ToArray();
            return $"{type} {cmdtext}(" + string.Join(',', parameters.Select(m => $"{m}=>@{m}")) + ");";
        }
    }
}
