using System.Text.Json.Serialization;
using static SLLibrary.JsonModel.Common;

namespace SLLibrary.JsonModel.Adm
{
    public class JsonSql
    {

        public class ComeInAdmin : SqlTemplate
        {
            public class Result
            {
                public int? client_id { get; set; }
            }

            public string login { get; set; }
            [JsonIgnore]
            private string _password { get; set; }
            public string password
            {
                get => string.IsNullOrEmpty(_password) ? null : LibHelper.Crypt(_password);
                set => _password = value;
            }
        }


        public class PrizeFundCategoryIns : SqlTemplate
        {
            public string name { get; set; }
        } 
        
        public class PrizeFundCategoryUpd : SqlTemplate
        {
            public int id { get; set; }
            public string name { get; set; }
        }

    }
}
