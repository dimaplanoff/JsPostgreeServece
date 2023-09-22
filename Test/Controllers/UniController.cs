using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SLLibrary;
using SLLibrary.SQL;
using System.Text;
using System.Text.Json;
using static SLLibrary.JsonModel.Common;

namespace UniApp.Controllers
{
    [ApiController]
    [Route("")]
    public class UniController : Controller
    {

        [HttpPost("post")]
        public JsonResult Post()
        {
            try
            {
                string body;
                using (var buff = HttpContext.Request.BodyReader.AsStream())
                    body = LibHelper.GetUtf8String(buff);

                if (string.IsNullOrEmpty(body))
                    throw new Exception("Can not read body");

                var data = JsonSerializer.Deserialize<Model.CPost>(body, Const.JsonSerializerWeb);
                var result = data.Write();

                return new JsonResult(result, LibHelper.JsonSerializerWeb);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Error = ex.Message }, LibHelper.JsonSerializerWeb);
            }
        }



        [HttpGet("get")]
        public JsonResult Get()
        {
            try
            {
                string name = string.Empty;
                var data = new Dictionary<string, object>();
                foreach (var q in Request.Query)
                {
                    if (q.Key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        name = q.Value;
                    else if (!data.ContainsKey(q.Key))
                        data.Add(q.Key, q.Value);
                }

                if (string.IsNullOrEmpty(name))
                    throw new Exception("Can not read procedure name");

                string authParams = string.Empty;
                foreach (var p in data)
                {
                    var pName = $"@p_{p.Key.ToLower()}";
                    authParams += $",{pName}";
                }

                if (authParams.Length > 0)
                    authParams = authParams.Substring(1);

                object res = null;
                using (Context context = new(Const.J.DbConnectionString))
                    res = context.Execute<object>(null, $@"select ""{name}""({authParams})", data.Values.ToList());

                return new JsonResult(res, LibHelper.JsonSerializerWeb);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Error = ex.Message }, LibHelper.JsonSerializerWeb);
            }
        }



        /// <summary>
        /// other fields will be added as additional parameters for sql procedure
        /// </summary>
        /// <param name="name">name of procedure (system field)</param>
        /// <param name="file">file data (system field)</param>
        /// <returns></returns>
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [HttpPost("excel")]
        public IActionResult FormExcel([FromForm] string name, [FromForm] IFormFile file)
        {
            try
            {

                byte[] bytes = new byte[file.Length];
                int b = -1, n = 0;
                using (var fStream = file.OpenReadStream())
                    while ((b = fStream.ReadByte()) != -1)
                    {
                        bytes[n] = (byte)b;
                        n++;
                    }


                var excelData = Excel.Parse(bytes, 1);
                if (excelData == null)
                    throw new Exception("can not read file");

                var json = Excel.ToDictionary(excelData);                

               var jsonResult = JsonSerializer.SerializeToElement(new { 
                   Parameters = Request.Form.Where(m => !new string[]{ "name", "file" }.ContainsIgnoreCase(m.Key)), 
                   FileName = file.FileName.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault(), Excel = json }, LibHelper.JsonSerializerWeb) ;

                var data = new Model.CPost { Name = name, Value = jsonResult };
                var result = data.Write();

                return new JsonResult(result, LibHelper.JsonSerializerWeb);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Error = ex.Message }, LibHelper.JsonSerializerWeb);
            }

        }
    }
}