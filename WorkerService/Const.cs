using SLLibrary;

namespace WorkerService
{
    public class ConstBase
    {
        public class TaskItem
        {
            public string Name { get; set; }
            public string MailTo { get; set; }
            public string FromDir { get; set; }
            public string ToDir { get; set; }
            public string[] Exceptions { get; set; }
            public string ScriptAfter { get; set; }
            public string ScriptBefore { get; set; }
        }


        public TaskItem[] Tasks { get; set; }
        public Mail.Config MailConf { get; set; }
    }

    public class Const
    {
       
        public static ConstBase Data { get; private set; }

        public static void Init(IConfiguration conf)
        {
            Data = conf.Get<ConstBase>();                
        }


    }
}
