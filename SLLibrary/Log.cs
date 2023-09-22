using System.Text;


namespace SLLibrary
{
    public class Log
    {
        public class Instance
        {

            public void LogInformation(Exception e)
            {
                Write(e);
            }

            public void LogInformation(string text)
            {
                Write(text);
            }

            public void Write(Exception e)
            {
                Log.Write(e);
            }

            public void Write(string text, bool consoleOnly = false)
            {
                if (consoleOnly)
                {
                    Console.WriteLine(text);
                    return;
                }
                Log.Write(text);
            }
        }


        private static Mutex sync = new();


        public static void Write(Exception e)
        {
            Write($"{e.Message}\r\n\t{e.StackTrace}");
        }


        public static void Write(string text)
        {

            Task.Factory.StartNew(() =>
            {
                sync.WaitOne();
                Console.WriteLine(text);

                try
                {
                    var log = Environment.OSVersion.Platform == PlatformID.Win32NT ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log") : $"/var/log/{AppDomain.CurrentDomain.FriendlyName}";
                    IO.CreateDirIfNotExist(log);
                    log = Path.Combine(log, DateTime.Now.ToString("yyyy-MM-dd--HH") + ".log");
                    using (var sw = new StreamWriter(log, true, Encoding.UTF8))
                        sw.WriteLine($"{DateTime.Now}: {text}\r\n\r\n");
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }
                finally
                {
                    sync.ReleaseMutex();
                }
            }, TaskCreationOptions.RunContinuationsAsynchronously);

        }
    }
}
