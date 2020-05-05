using System;
using MailClient.MVVM;
using MailLib;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Windows;
using System.Windows.Input;

namespace MailClient.ViewModels
{
    public class MailViewModel : BaseViewModel
    {
        #region Properties

        private List<Mail> _mails;
        public List<Mail> Mails
        {
            get { return _mails; }
            set { SetProperty(ref _mails, value); }
        }


        private List<string> _folders;
        public List<string> Folders
        {
            get { return _folders; }
            set { SetProperty(ref _folders, value); }
        }


        private string _selectedFolder;
        public string SelectedFolder
        {
            get { return _selectedFolder; }
            set { SetProperty(ref _selectedFolder, value); }
        }


        private Mail _selectedMail;
        public Mail SelectedMail
        {
            get { return _selectedMail; }
            set { SetProperty(ref _selectedMail, value); }
        }


        private string _selectedMailText;
        public string SelectedMailText
        {
            get { return _selectedMailText; }
            set { SetProperty(ref _selectedMailText, value); }
        }

        private bool _isBrowserVisible = false;
        public bool IsBrowserVisible
        {
            get { return _isBrowserVisible; }
            set { SetProperty(ref _isBrowserVisible, value); }
        }

        private bool _isInputMailVisible = false;
        public bool IsInputMailVisible
        {
            get { return _isInputMailVisible; }
            set { SetProperty(ref _isInputMailVisible, value); }
        }

        private string _mailTo;
        public string MailTo
        {
            get { return _mailTo; }
            set { SetProperty(ref _mailTo, value); }
        }

        private string _mailSubject;
        public string MailSubject
        {
            get { return _mailSubject; }
            set { SetProperty(ref _mailSubject, value); }
        }

        private string _mailContent;
        public string MailContent
        {
            get { return _mailContent; }
            set { SetProperty(ref _mailContent, value); }
        }
        private string _sendMailError;
        public string SendMailError
        {
            get { return _sendMailError; }
            set { SetProperty(ref _sendMailError, value); }
        }
        #endregion

        #region Non Property private fields

        private Mailbox _mailbox;

        #endregion

        #region Commands

        public ICommand SelectFolderCommand { get; private set; }
        public ICommand SelectMailCommand { get; private set; }
        public ICommand DeleteMailCommand { get; private set; }
        public ICommand DeleteAllMailCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand NewMailCommand { get; private set; }
        public ICommand SendMailCommand { get; private set; }

        #endregion

        public MailViewModel()
        {
            SelectFolderCommand = new AsyncDelegateCommand(GetMailHeaders);
            SelectMailCommand = new AsyncDelegateCommand(GetMailBody);
            DeleteMailCommand = new AsyncDelegateCommand(DeleteMail);
            DeleteAllMailCommand = new AsyncDelegateCommand(DeleteAllMail);
            RefreshCommand = new AsyncDelegateCommand(Refresh);
            NewMailCommand = new AsyncDelegateCommand(NewMail);
            SendMailCommand = new AsyncDelegateCommand(SendMail);

            using (new BusyIndicator())
            {
                try
                {
                    _mailbox = Mailbox.GetInstance();
                    Folders = _mailbox.GetFolders();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,"Произошла ошибка!",MessageBoxButton.OK,MessageBoxImage.Error);
                }
            }

        }

        #region Private Methods

        private void GetMailHeaders(object param)
        {
            if (SelectedFolder != "")
            {
                IsBrowserVisible = false;
                using (new BusyIndicator())
                {
                    try
                    {
                        _mailbox.SelectFolder(SelectedFolder);
                        Mails = _mailbox.GetMailHeaders();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Произошла ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                IsBrowserVisible = !IsInputMailVisible && SelectedMailText != null;
            }
        }

        private void GetMailBody(object param)
        {
            if (SelectedMail == null)
                return;

            using (new BusyIndicator())
            {
                IsBrowserVisible = false;

                try
                {
                    SelectedMailText = _mailbox.GetMailBody(SelectedMail.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Произошла ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsInputMailVisible = false;
                IsBrowserVisible = !IsInputMailVisible;
            }

        }

        private void DeleteMail(object param)
        {
            if (SelectedMail == null)
                return;

            using (new BusyIndicator())
            {
                IsBrowserVisible = false;

                try
                {
                    _mailbox.DeleteMail(SelectedMail.Id);
                    Mails = _mailbox.GetMailHeaders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Произошла ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsBrowserVisible = !IsInputMailVisible;
            }

        }

        private void DeleteAllMail(object param)
        {
            using (new BusyIndicator())
            {
                IsBrowserVisible = false;

                try
                {
                    foreach (Mail mail in Mails)
                    {
                        _mailbox.DeleteMail(mail.Id);
                    }

                    Mails = _mailbox.GetMailHeaders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Произошла ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsBrowserVisible = !IsInputMailVisible;
            }
        }

        private void Refresh(object param)
        {
            using (new BusyIndicator())
            {
                IsBrowserVisible = false;

                try
                {
                    Mails = _mailbox.GetMailHeaders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Произошла ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                IsBrowserVisible = !IsInputMailVisible;
            }
        }

        private void NewMail(object param)
        {
            IsBrowserVisible = false;
            IsInputMailVisible = true;
        }

        private void SendMail(object param)
        {
            using (new BusyIndicator())
            {
                try
                {
                    if (_mailbox.SendMail(MailTo, MailSubject, MailContent))
                        SendMailError = "";
                    else
                        SendMailError = "Не удалось отправить письмо!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Произошла ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }
        #endregion

    }
}
