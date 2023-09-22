using System.Diagnostics;
using System.Text;

namespace WorkerService
{
    public class Worker : BackgroundService
    {



        private bool NeedToRewrite(string objectName, string[] exceptions)
        {
            if (objectName == checkFileName)
                return false;

            if (exceptions.Contains(objectName))
                return false;

            foreach (var e in exceptions)
            {
                if (e.StartsWith('*'))
                    if (objectName.EndsWith(e.Substring(1)))
                        return false;

                if (e.EndsWith('*'))
                    if (objectName.StartsWith(e.Substring(0, e.Length - 1)))
                        return false;
            }

            return true;
        }

        private void ReWrireDir(string fromDir, string toDir, string[] exceptions)
        {

            SLLibrary.IO.CreateDirIfNotExist(toDir);


            var fromFiles = Directory.GetFiles(fromDir).ToDictionary(a => Path.GetFileName(a), b => b);
            var toFiles = Directory.GetFiles(toDir).ToDictionary(a => Path.GetFileName(a), b => b);

            foreach (var fromFile in fromFiles)
            {

                if (!NeedToRewrite(fromFile.Key, exceptions))
                    continue;

                var newFilePath = Path.Combine(toDir, fromFile.Key);
                try
                {
                    File.Copy(fromFile.Value, newFilePath, true);
                    mailMessage.AppendLine($"{DateTime.Now}: file: {fromFile.Value} -> {newFilePath}");
                }
                catch (Exception e)
                {
                    errors.Add(new Exception($"{fromFile.Key}: {e.Message}"));
                }



            }

            var dirs = Directory.GetDirectories(fromDir);
            var dirsDict = dirs.ToDictionary(m => m.Split(pathSeparator).Last(m => !string.IsNullOrEmpty(m)), n => n);
            foreach (var dir in dirsDict)
            {
                if (!NeedToRewrite(dir.Key, exceptions))
                    continue;
                var toDirNext = Path.Combine(toDir, dir.Key);
                ReWrireDir(dir.Value, toDirNext, exceptions);
            }

        }

        private void Run(string script)
        {
            if (string.IsNullOrEmpty(script))
                return;

            mailMessage.AppendLine($"{DateTime.Now}: run script: {script}");

            try
            {
                using (Process process = new())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = isWin ? "cmd.exe" : "/bin/bash",
                        Arguments = isWin ? $"/C {script}" : $@"-c ""{script}""",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    };
                    process.Start();
                    process.WaitForExit();
                    var result = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(result))
                        mailMessage.AppendLine($"{DateTime.Now}: script result: {result}");
                    if (!string.IsNullOrEmpty(error))
                        throw new Exception(error);
                }
            }
            catch (Exception e)
            {
                mailMessage.AppendLine($"{DateTime.Now}: script error: {e.Message}");
                throw;
            }
        }

        private bool isWin => Environment.OSVersion.Platform == PlatformID.Win32NT;
        private const string checkFileName = "update";
        private char pathSeparator => Path.DirectorySeparatorChar;//isWin ? '\\' : '/';

        private StringBuilder mailMessage = null;
        private List<Exception> errors = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var task in Const.Data.Tasks)
                {
                    mailMessage = new();
                    errors.Clear();
                    string subject = task.Name;

                    try
                    {
                        if (Directory.Exists(task.FromDir))
                        {
                            string checkFile = Path.Combine(task.FromDir, checkFileName);
                            if (File.Exists(checkFile))
                            {
                                mailMessage.AppendLine($"{DateTime.Now}: START TASK: {task.FromDir} -> {task.ToDir}");

                                Run(task.ScriptBefore);
                                ReWrireDir(task.FromDir, task.ToDir, task.Exceptions ?? new string[0]);
                                Run(task.ScriptAfter);
                                File.Delete(checkFile);
                                mailMessage.AppendLine($"{DateTime.Now}: TASK DONE");
                                if (errors.Count == 0)
                                    subject = $"{subject}: ok";
                                else
                                    throw new Exception("Task finished with errors");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var historyLog = mailMessage.ToString();
                        mailMessage = new();

                        mailMessage.AppendLine($"{DateTime.Now}: {e.Message}");
                        if (!string.IsNullOrEmpty(e.StackTrace))
                            mailMessage.AppendLine(e.StackTrace);
                        if (e.InnerException != null)
                        {
                            mailMessage.AppendLine(e.InnerException.Message);
                            if (!string.IsNullOrEmpty(e.InnerException.StackTrace))
                                mailMessage.AppendLine(e.InnerException.StackTrace);
                        }

                        foreach (var err in errors)
                            mailMessage.AppendLine($" - {err.Message}");

                        mailMessage.AppendLine();
                        mailMessage.AppendLine("---------- LOG ----------");
                        mailMessage.AppendLine(historyLog);
                        mailMessage.AppendLine("---------- LOG ----------");

                        subject = $"{subject}: error";

                        SLLibrary.Log.Write(e);
                    }


                    if (mailMessage?.Length > 0)
                        SLLibrary.Mail.Send(Const.Data.MailConf, subject, task.MailTo, mailMessage.ToString());


                }
                GC.Collect();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}