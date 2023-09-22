

namespace PublicWebService.JsonModel.VK
{

    public class Error
    {
        public int error_code { get; set; }
        public string error_msg { get; set; }
        public string error_text { get; set; }
    }

    public class Common<T>
    {
        public T response { get; set; }
        public Error error { get; set; }

        public Common(T data)
        {
            response = data;
        }        

        public Common()
        {
        }

    }

    public class UsersGet
    {
        public class District
        {
            public int id { get; set; }
            public string title { get; set; }
        }

        public long id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string phone { get; set; }
        public District city { get; set; }
        public District country { get; set; }

    }

    public class Auth
    {
        public string access_token { get; set; }
        public string code { get; set; }
        public long user_id { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
    } 

}
