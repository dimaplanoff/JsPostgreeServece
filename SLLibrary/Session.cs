using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace SLLibrary.Identity
{
    public class UserAuth
    {
        public int? client_id { get; set; }
    }
}


namespace SLLibrary
{

    public class JwtSession
    {
        private JwtSecurityTokenHandler handler = new();
        private long GetCurrentExt => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        private TokenValidationParameters validationParameters;
        private string connStr, domain;
        private SecurityKey securityKey;
        private int jwtLiveTime;


        private static object sync = new();
        private static JwtSession _this = null;

        private JwtSession(string connStr, string domain, SecurityKey securityKey, int jwtLiveTime) 
        {
            this.connStr = connStr;
            this.domain = domain;
            this.securityKey = securityKey;
            this.jwtLiveTime = jwtLiveTime;
            this.validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudiences = new string[] { "public" },
                ValidateIssuer = false,
                RequireExpirationTime = false,
                IssuerSigningKey = this.securityKey                                
            };
        }

        public static JwtSession Instance(string connStr, string domain, SecurityKey securityKey, int jwtLiveTime)
        {
            lock (sync)
            {
                if (_this == null)
                    _this = new JwtSession(connStr, domain, securityKey, jwtLiveTime);

                return _this;
            }
        }

       
        public bool TryCreateKey(string ip, out string key)
        {
            try
            {
                key = LibHelper.NewKey;
                using (SQL.Context contex = new(connStr))
                {
                    var res = contex.Execute<JsonModel.Common.StandartResult>(null, @"select ""fndAddKey""(@p_key, @p_ip)", key, ip);
                    if (res.status != 1)
                        throw new Exception(res?.message ?? "Key session storage error");
                }

                return true;
            }
            catch
            {
                key = null;
                return false;
            }
        }


        private bool TrySetToken(string key, string token, Identity.UserAuth user, string ip = null)
        {
            try
            {
                using (SQL.Context contex = new(connStr))
                {
                    var res = contex.Execute<JsonModel.Common.StandartResult>(null, @"select ""fndUpdateKeyToken""(@p_key, @p_token, @p_client_id, @p_ip)", key, token, user?.client_id, ip);
                    if (res.status != 1)
                        throw new Exception(res?.message ?? "Token session storage error");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }      


        private string CreateToken(string key, string audience, Identity.UserAuth user = null)
        {
            if (string.IsNullOrEmpty(audience))
                audience = "public";            

            string jsonload = JsonSerializer.Serialize(user?.client_id > 0 ? user : new object());

            var token = new JwtSecurityToken(
                    issuer: domain,
                    audience: audience,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(jwtLiveTime)),
                    signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256),
                    claims: new[] { new Claim("user", jsonload), new Claim("key", key), }
                    );            
            return new JwtSecurityTokenHandler().WriteToken(token);         
        }


        private bool Validate(string token, out string key, out Identity.UserAuth user)
        {
            user = default;

            try
            {
                var claim = handler.ValidateToken(token, validationParameters, out SecurityToken security);

                key = claim.FindFirst("key")?.Value;
                if (string.IsNullOrEmpty(key))
                    throw new Exception("Empty key");

                var userStr = claim.FindFirst("user")?.Value;
                if (!string.IsNullOrEmpty(userStr))
                    user = JsonSerializer.Deserialize<Identity.UserAuth>(userStr);

                return true;
            }
            catch
            {
                key = null;
                return false;
            }
        }

        //возвращаем true или false в зависимости от всех проверок, но пытаемся получить ключ в любом случае для продления токена
        public bool CheckToken(string token, out string key, out Identity.UserAuth user)
        {

            try
            {
                token = token.Split(" ").OrderByDescending(m => m.Length).FirstOrDefault();
                if (Validate(token, out key, out user))
                    return true;

                if (string.IsNullOrEmpty(key))
                {
                    var security = handler.ReadJwtToken(token);

                    if (security?.Issuer == domain && security.Audiences.Contains("public"))
                    {

                        var expCliam = security.Claims.FirstOrDefault(m => m.Type == "exp");
                        var keyCliam = security.Claims.FirstOrDefault(m => m.Type == "key");

                        if (expCliam != null && keyCliam != null)
                        {
                            key = keyCliam.Value;
                            var userStr = security.Claims.FirstOrDefault(m => m.Type == "user")?.Value;
                            if (!string.IsNullOrEmpty(userStr))
                                user = JsonSerializer.Deserialize<Identity.UserAuth>(userStr);

                            return false;
                        }
                    }
                }
                throw new Exception("Invalid token");
            }
            catch
            {
                key = null;
                user = default;
                return false;
            }
        }


        public bool TryUpdateToken(string key, string ip, Identity.UserAuth user, out string token)
        {
            try
            {
                token = CreateToken(key, null, user);
                if(TrySetToken(key, token, user, ip))
                    return true;
                throw new Exception("Can not update token");
            }
            catch
            {
                token = null;
                return false;
            }
        }

    }
}
