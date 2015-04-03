using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace MonitorNPRCH
{
    public class MailClass
    {
        
        public static bool SendTextMail(string subject, string message) {
            try {
                if (!Settings.single.SendErrorMail)
                    return true;
                System.Net.Mail.MailMessage mess = new System.Net.Mail.MailMessage();

                mess.From = new MailAddress(Settings.single.SMTPFrom);
                mess.Subject = subject; mess.Body = message;
                char[] sep = { ';' };
                string[] addrs = Settings.single.SMTPErrorTo.Split(sep);
                foreach (string mail in addrs) {
                    if (mail.Length > 0) {
                        mess.To.Add(mail);
                    }
                }

                mess.SubjectEncoding = System.Text.Encoding.UTF8;
                mess.BodyEncoding = System.Text.Encoding.UTF8;
                mess.IsBodyHtml = true;
                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(Settings.single.SMTPServer, Settings.single.SMTPPort);
                client.EnableSsl = true;
                if (Settings.single.SMTPUser.Length > 0) {
                    client.UseDefaultCredentials = false;                    
                    client.Credentials = new System.Net.NetworkCredential(Settings.single.SMTPUser, Settings.single.SMTPPassword, Settings.single.SMTPDomain);
                }
                else {
                    client.UseDefaultCredentials = true;
                }
                
                // Отправляем письмо
                client.Send(mess);
                return true;
            }
            catch (Exception e) {
                Logger.Info("Ошибка при отправке почты");
                Logger.Info(e.ToString());
            }
            return false;
        }

    }
}
