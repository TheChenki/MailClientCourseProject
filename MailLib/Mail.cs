using System;

namespace MailLib
{
    public class Mail
    {
        public int Id { get; internal set; }
        public string Subject { get; internal set; }
        public string From { get; internal set; }
        public DateTime Date { get; internal set; }

    }
}
