using MailLib.DAL.Imap;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MailLib.DAL.Smtp;

namespace MailLib
{
    public class Mailbox
    {
        public string Folder { get; private set; }

        private Imap imap;
        private Smtp smtp;

        private Dictionary<string, string> folders;

        #region Singleton

        private static Mailbox _instance;
        private Mailbox()
        {
            folders = new Dictionary<string, string>();
            imap = new Imap();
            smtp = new Smtp();
        }

        public static Mailbox GetInstance()
        {
            if (_instance == null)
                _instance = new Mailbox();

            return _instance;
        }

        #endregion
        public void ConnectToImapServer(string host, int port, bool ssl = false)
        {
            imap.Connect(host, port, ssl);
        }

        public void ConnectToSmtpServer(string host, int port, bool ssl = false)
        {
            smtp.Connect(host, port, ssl);
        }

        public bool ImapLogin(string login, string password)
        {
            return imap.Login(login, password);
        }

        public bool SmtpLogin(string login, string password)
        {
            return smtp.Login(login, password);
        }

        public bool SendMail(string to, string subject, string content)
        {
            return smtp.SendMail(to, subject, content);
        }

        public void SelectFolder(string folder)
        {
            if (imap.SelectFolder(folders[folder]))
            {
                Folder = folder;
            }
            else
            {
                Folder = null;
                throw new ArgumentException("Запрашиваемая папка не существует!");
            }
        }

        public bool DeleteMail(int uid)
        {
            return imap.DeleteMail(uid);
        }

        public List<string> GetFolders()
        {
            List<string> response;
            imap.GetFolders(out response);

            folders.Clear();

            Regex regex = new Regex("\\* LIST \\((?<Markers>.*)\\) \"\\/\" (?<folder>.*?)$");
            List<string> prefixes = new List<string>();

            for (int i = 0; i < response.Count - 1; i++)
            {
                Match match = regex.Match(response[i]);
                if (match.Groups["Markers"].Value.Contains("HasChildren"))
                {
                    string decoded = Utils.UTF7Decode(match.Groups["folder"].Value);
                    decoded = decoded.Trim('\"');
                    prefixes.Add(decoded);
                }
                else
                {
                    string key = Utils.UTF7Decode(match.Groups["folder"].Value).Trim('\"');
                    string value = match.Groups["folder"].Value.Trim('\"');
                    foreach (string pref in prefixes)
                    {
                        if (key.StartsWith(pref))
                        {
                            key = key.Replace(pref + "/", "");
                        }
                    }
                    folders.Add(key, value);
                }
            }
            return new List<string>(folders.Keys);
        }

        public List<Mail> GetMailHeaders()
        {
            List<Mail> mails = new List<Mail>();

            if (Folder == null)
                throw new Exception("Не выбрана папка!");

            List<string> response;
            if (imap.GetMailHeaders(out response))
            {
                int line = 0;
                while (line < response.Count - 1)
                {
                    ParseHeaders(response, ref mails, ref line);
                }
            }

            return mails;
        }

        public string GetMailBody(int uid)
        {
            List<string> response;
            string result = "";
            if (imap.GetMail(uid, out response))
            {
                result = ParseBody(response);
            }
            return result;
        }

        #region Parse methods
        //Принимает List с заголовками(Subject Date From) писем
        //Парсит только одно письмо
        //Через Line возвращает индекс начала заголовков следующего письма
        private void ParseHeaders(List<string> data, ref List<Mail> mails, ref int line)
        {
            string header = string.Empty;
            string str = string.Empty;
            bool replace = false;
            Mail mail = new Mail();

            for (int i = line; i < data.Count; i++)
            {
                str = data[i];

                if (i == (data.Count - 2) && str.StartsWith("*"))
                {
                    line = data.Count;
                    return;
                }


                // ) - Конец заголовков одного письма
                Regex regex = new Regex(@"(UID )(?<uid>[0-9]*)(\))");
                Match match = regex.Match(str);
                if (str == ")" || match.Success)
                {
                    mails.Add(mail);
                    line = i + 1;

                    if (match.Success)
                    {
                        mail.Id = int.Parse(match.Groups["uid"].Value);
                    }

                    return;
                }

                if (str == "")
                    continue;

                // * - некоторая доп информация от сервера. 
                //Отсюда необходим порядовый номер письма в ящике
                if (str.StartsWith("*"))
                {
                    regex = new Regex("(UID) (?<uid>[0-9]*) (BODY)");
                    match = regex.Match(str);
                    if (match.Success)
                    {
                        mail.Id = int.Parse(match.Groups["uid"].Value);
                    }
                    continue;
                }

                replace = false;
                if (str.StartsWith("Subject: "))
                {
                    header = "Subject: ";
                    replace = true;
                }
                else if (str.StartsWith("Date: "))
                {
                    header = "Date: ";
                    replace = true;

                    int index = str.IndexOf("(");
                    if (index != -1)
                    {
                        str = str.Substring(0, index);
                    }
                }
                else if (str.StartsWith("From: "))
                {
                    header = "From: ";
                    replace = true;
                }

                if (replace)
                {
                    str = str.Replace(header, string.Empty);
                }

                str = Utils.DecodeEncodedLine(str);

                switch (header)
                {
                    case "Subject: ": mail.Subject += str; break;
                    case "Date: ": mail.Date = DateTime.Parse(str); break;
                    case "From: ": mail.From += str; break;
                    default:
                        continue;
                        //throw new ArgumentException("Unknown Header!");
                }
            }
        }

        private string ParseBody(List<string> data)
        {
            data.RemoveAt(0);
            data.RemoveAt(data.Count - 1);
            data.RemoveAt(data.Count - 1);

            string str = String.Join("\r\n", data);

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;

            MimeMessage message = MimeMessage.Load(stream);

            string result;
            if (message.HtmlBody == null)
            {
                result = message.TextBody;
            }
            else
            {
                result = message.HtmlBody;
            }

            return result;

        }
        #endregion
    }
}
