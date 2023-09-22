using static SLLibrary.JsonModel.Common;
using System.Text.Json.Serialization;

namespace SLLibrary.JsonModel
{
    public class JsonSql
    {                            

        public class Registr : SqlTemplate
        {
            public class Result
            {
                public string guid { get; set; }
            }

            public string email { get; set; }

            [JsonIgnore]
            private string _password { get; set; }
            public string password {
                get => string.IsNullOrEmpty(_password) ? null : LibHelper.Crypt(_password);
                set => _password = value; }

          
        }

        public class Confirm : SqlTemplate
        {
            public string Guid { get; set; }
        }

        public class ComeIn : SqlTemplate
        {
            public class Result
            {
                public int? client_id { get; set; }                
            }

            public string email { get; set; }
            [JsonIgnore]
            private string _password { get; set; }
            public string password
            {
                get => string.IsNullOrEmpty(_password) ? null : LibHelper.Crypt(_password);
                set => _password = value;
            }
        }

        public class ComeInVk : SqlTemplate
        {
            public class Result
            {
                public int? client_id { get; set; }
            }

            public string name { get; set; }
            public string surname { get; set; }
            public DateTime? date_birth { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string social_id { get; set; }
            public string social_token { get; set; }
            public string langs { get; set; }
        }

        public class PasswordRecoverGetGuid 
        {
            public class Result
            {
                public string guid { get; set; }
            }

            public string email { get; set; }
            public string redirect { get; set; }
        }

        public class PasswordRecover : SqlTemplate
        {
            public string guid { get; set; }
            [JsonIgnore]
            private string _password { get; set; }
            public string password
            {
                get => string.IsNullOrEmpty(_password) ? null : LibHelper.Crypt(_password);
                set => _password = value;
            }
        }

        public class PasswordChange : SqlTemplate
        {

            [JsonIgnore]
            private string _old_password { get; set; }
            public string old_password
            {
                get => string.IsNullOrEmpty(_old_password) ? null : LibHelper.Crypt(_old_password);
                set => _old_password = value;
            }

            [JsonIgnore]
            private string _password { get; set; }
            public string password
            {
                get => string.IsNullOrEmpty(_password) ? null : LibHelper.Crypt(_password);
                set => _password = value;
            }
        }

    }
}
