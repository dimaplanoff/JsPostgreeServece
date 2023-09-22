using Microsoft.IdentityModel.Tokens;
using SLLibrary;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;


namespace UniApp
{
    public class JConst
    {
        public string DbConnectionString { get; set; }      
    }

    public class Const 
    {
        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        private Const()
        { 
        }

        private static Const _this = null;
        private static object sync = new ();        
        public static Const Entity
        {
            get 
            {
                lock (sync)
                {
                    if(_this == null)
                        _this = new Const();
                    return _this;
                }
            }
        }

        public void Init(IConfiguration config)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            J = config.Get<JConst>();            
        }

        public static JConst J { get; private set; }
        public static readonly JsonSerializerOptions JsonSerializerWeb = LibHelper.JsonSerializerWeb;

    }
}
