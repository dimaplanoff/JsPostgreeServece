using System.Text;
using MimeKit;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace SLLibrary
{
    public class Mail
    {
        public class Config
        {
            public string Server { get; set; }
            public int Port { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string From { get; set; }
        }


        public static void Send(Config config, string subject, string to, string text, bool html = false)
        {
            if (string.IsNullOrEmpty(to))
                return;

            try
            {
                using (MimeMessage mail = new MimeMessage())
                {

                    mail.From.Add(MailboxAddress.Parse(config.From));
                    mail.To.Add(MailboxAddress.Parse(to));
                    mail.Subject = subject;

                    var mailBody = new StringBuilder();
                    mailBody.Append(text);
                    var builder = new BodyBuilder();
                    //builder.Attachments.Add($"attachment.txt", Encoding.UTF8.GetBytes(""));


                    if (html)
                        builder.HtmlBody = mailBody.ToString();
                    else
                        builder.TextBody = mailBody.ToString();

                    mail.Body = builder.ToMessageBody();

                    using (SmtpClient smtpClient = new SmtpClient())
                    {
                        smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        smtpClient.Connect(config.Server, config.Port, SecureSocketOptions.Auto);
                        smtpClient.Authenticate(config.User, config.Password);
                        smtpClient.Send(mail);
                        smtpClient?.Disconnect(true);
                    }

                }
            }
            catch (Exception e) 
            {
                Log.Write(e);
            }
        }
    }
}
