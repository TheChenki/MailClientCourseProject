using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MailLib.DAL.Smtp
{
    public class Smtp
    {
        private string host;
        private int port;
        private bool useSsl;

        private string login;
        private string password;
        //Connection and R/W streams
        private TcpClient tcp;
        private SslStream ssl;
        private StreamReader reader;
        private StreamWriter writer;
        //Sync object
        private object locker = new object();

        public Smtp()
        {
            tcp = new TcpClient();
        }

        private List<string> SendAndWaitForResponse(string command, out bool success, string code)
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

                        Regex regex = new Regex(@"^([0-9]+ )");
                        Match match = regex.Match(line);

                        if (match.Success)
                        {
                            stop = true;
                            success = line.StartsWith(code);
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

        #region Public Commands
        public bool Login(string login, string password)
        {
            bool success = false;
            this.login = login;
            this.password = password;

            string command = "EHLO " + host;
            SendAndWaitForResponse(command, out success, "250");

            if (!success)
                return false;

            command = "AUTH PLAIN " + Utils.Base64Encode("\0" + login + "\0" + password);
            SendAndWaitForResponse(command, out success,"235");
            
            return success;
        }

        public bool SendMail(string to, string subject, string content)
        {
            bool success = false;

            string command = "EHLO " + host;
            SendAndWaitForResponse(command, out success, "250");

            if (!success)
                return false;

            command = "MAIL FROM: <" + login + ">";
            SendAndWaitForResponse(command, out success, "250");

            if (!success)
                return false;

            command = "RCPT TO: <" + to + ">";
            SendAndWaitForResponse(command, out success, "250");

            if (!success)
                return false;

            command = "DATA";
            SendAndWaitForResponse(command, out success, "354");

            if (!success)
                return false;

            command = "From: " + login + "\r\nSubject: " + subject + "\r\n\r\n" + content + "\r\n.";

            SendAndWaitForResponse(command, out success, "250");
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
                if (!line.StartsWith("220 "))
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
