namespace MailLib.DAL.Imap
{
    internal class ImapStrings
    {
        public const string CommandPrefix = "$ ";

        public const string Response_OK = CommandPrefix + "OK";
        public const string Response_NO = CommandPrefix + "NO";
        public const string Response_BAD = CommandPrefix + "BAD";

        public const string Command_LOGIN = CommandPrefix + "LOGIN";
        public const string Command_LOGOUT = CommandPrefix + "LOGOUT";
        public const string Command_SELECT = CommandPrefix + "SELECT";
        public const string Command_FETCH = CommandPrefix + "UID FETCH";
        public const string Command_LIST = CommandPrefix + "LIST";
        public const string Command_STORE = CommandPrefix + "UID STORE";
        public const string Command_EXPUNGE = CommandPrefix + "EXPUNGE";
    }
}
