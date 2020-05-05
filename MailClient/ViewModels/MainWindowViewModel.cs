using MailClient.MVVM;
using System;
namespace MailClient.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Properties
        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        private BaseViewModel _viewModel;

        public BaseViewModel ViewModel
        {
            get { return _viewModel; }
            set { SetProperty(ref _viewModel, value); }
        }
        #endregion

        public MainWindowViewModel()
        {
            BusyIndicator.Busy += OnBusy;
            BusyIndicator.Free += OnFree;
            Navigator.Navigate += OnNavigate;

            ViewModel = new LoginViewModel();
        }


        private void OnBusy(object sender, EventArgs e)
        {
            IsBusy = true;
        }

        private void OnFree(object sender, EventArgs e)
        {
            IsBusy = false;
        }

        private void OnNavigate(object sender, EventArgs e)
        {
            NavigationEventArgs args = e as NavigationEventArgs;

            ViewModel = args.ViewModel;
        }
    }
}
