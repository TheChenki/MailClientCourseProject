using MailClient.MVVM;
using MailLib;
using System;
using System.Windows.Input;
using MailLib.DAL.Smtp;

namespace MailClient.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        #region Properties

        private string _login = String.Empty;
        public string Login
        {
            get { return _login; }
            set { SetProperty(ref _login, value); }
        }

        private string _password = String.Empty;
        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        private string _imap = String.Empty;
        public string Imap
        {
            get { return _imap; }
            set { SetProperty(ref _imap, value); }
        }

        private string _smtp = String.Empty;
        public string Smtp
        {
            get { return _smtp; }
            set { SetProperty(ref _smtp, value); }
        }

        private string _error = String.Empty;
        public string Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }

        #endregion

        #region Non Property private fields

        private Mailbox _mailbox;

        #endregion

        #region Commands

        public ICommand LoginCommand { get; private set; }

        #endregion

        public LoginViewModel()
        {
            _mailbox = Mailbox.GetInstance();

            LoginCommand = new AsyncDelegateCommand(ConnectAndLogin);
        }

        private void ConnectAndLogin(object param)
        {
            try
            {
                using (new BusyIndicator())
                {
                    string login = Login.Trim();
                    string password = Password.Trim();
                    string imap = Imap.Trim();
                    string smtp = Smtp.Trim();

                    if (login == string.Empty || password == string.Empty || imap == string.Empty || smtp == string.Empty)
                        return;

                    _mailbox.ConnectToImapServer(imap, 993, true);
                    _mailbox.ConnectToSmtpServer(smtp,465, true);

                    if (_mailbox.ImapLogin(login, password) && _mailbox.SmtpLogin(login,password))
                        Navigator.NavigateTo(new MailViewModel());
                    else
                        Error = "Неверный логин или пароль!";
                }
            }
            catch (Exception ex)
            {
                Error = "Во время соединения произошла ошибка. Проверьте правильность введенных данных!";
            }
        }
    }
}
