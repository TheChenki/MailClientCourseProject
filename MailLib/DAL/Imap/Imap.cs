using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace MailLib.DAL.Imap
{
    public class Imap
    {

        private string host;
        private int port;
        private bool useSsl;
        //Connection and R/W streams
        private TcpClient tcp;
        private SslStream ssl;
        private StreamReader reader;
        private StreamWriter writer;
        //Sync object
        private object locker = new object();

        public Imap()
        {
            tcp = new TcpClient();
        }

        private List<string> SendAndWaitForResponse(string command, out bool success)
        {
            lock (locker)
            {
                List<string> response = new List<string>();
                string line;
                success = false;

                try
                {
                    if (!tcp.Connected)
                        Connect();

                    writer.WriteLine(command);
                    writer.Flush();

                    bool stop = false;

                    do
                    {

                        line = reader.ReadLine();
                        response.Add(line);

                        if (line.StartsWith(ImapStrings.Response_OK))
                        {
                            stop = true;
                            success = true;
                        }

                        if (line.StartsWith(ImapStrings.Response_NO) || line.StartsWith(ImapStrings.Response_BAD))
                        {
                            stop = true;
                            success = false;
                        }
                    } while (!stop);
                }
                catch (Exception ex)
                {
                    //Игнорирование таймаута и т.д.
                }

                return response;
            }
        }

        #region Public commands
        public bool Login(string login, string password)
        {
            bool success = false;
            string command = ImapStrings.Command_LOGIN + " " + login + " " + password;
            SendAndWaitForResponse(command, out success);

            return success;
        }

        public bool Logout()
        {
            bool success = false;
            string command = ImapStrings.Command_LOGOUT;
            SendAndWaitForResponse(command, out success);

            return success;
        }

        public bool SelectFolder(string folder)
        {
            bool success = false;
            string command = ImapStrings.Command_SELECT + " " + "\"" + folder + "\"";
            SendAndWaitForResponse(command, out success);

            return success;
        }

        public bool DeleteMail(int uid)
        {
            bool success = false;
            string command = ImapStrings.Command_STORE + " " + uid + " +FLAGS.SILENT (\\Deleted)"; //UID STORE (int uid) + FLAGS.SILENT (\Deleted)
            SendAndWaitForResponse(command, out success);

            if (success)
            {
                command = ImapStrings.Command_EXPUNGE;
                SendAndWaitForResponse(command, out success);
            }

            return success;
        }

        public bool GetFolders(out List<string> response)
        {
            bool success = false;
            string command = ImapStrings.Command_LIST + " \"\" \"*\""; //LIST "" "*"
            response = SendAndWaitForResponse(command, out success);

            return success;
        }

        public bool GetMailHeaders(out List<string> response)
        {
            bool success = false;
            string command = ImapStrings.Command_FETCH + " 1:* " + "BODY[HEADER.FIELDS (From Date Subject)]";
            response = SendAndWaitForResponse(command, out success);

            return success;
        }

        public bool GetMail(int index, out List<string> response)
        {
            bool success = false;
            string command = ImapStrings.Command_FETCH + $" {index} " + "BODY[]";
            response = SendAndWaitForResponse(command, out success);

            return success;
        }

        #endregion

        #region Connect
        public void Connect(string host, int port, bool ssl)
        {
            if (tcp.Connected)
                tcp.Close();

            this.host = host;
            this.port = port;
            this.useSsl = ssl;
            Connect();

        }

        private void Connect()
        {
            tcp.Dispose();
            tcp = new TcpClient();
            tcp.ReceiveTimeout = 2000;
            IAsyncResult result = tcp.BeginConnect(host, port, null, null);
            WaitHandle wh = result.AsyncWaitHandle;
            try
            {
                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    tcp.Close();
                    throw new TimeoutException($"{host}:{port} не отвечает!");
                }

                tcp.EndConnect(result);
            }
            finally
            {
                wh.Close();
            }

            if (useSsl)
            {
                ssl = new SslStream(tcp.GetStream());
                ssl.AuthenticateAsClient(host);

                reader = new StreamReader(ssl);
                writer = new StreamWriter(ssl);
            }
            else
            {
                reader = new StreamReader(tcp.GetStream());
                writer = new StreamWriter(tcp.GetStream());
            }

            try
            {
                string line = reader.ReadLine();
                if (!line.StartsWith("* OK"))
                    throw new ArgumentException($"{host}:{port} ответил неожиданно!");
            }
            catch (IOException)
            {
                throw new ArgumentException($"Возможно, {host}:{port} требует SSL");
            }

        }
        #endregion
    }
}
