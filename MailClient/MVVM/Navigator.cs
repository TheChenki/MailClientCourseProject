using System;

namespace MailClient.MVVM
{
    public class Navigator
    {
        public static event EventHandler Navigate;

        public static void NavigateTo(BaseViewModel viewModel)
        {
            Navigate.Invoke(null, new NavigationEventArgs(viewModel));
        }
    }

    public class NavigationEventArgs : EventArgs
    {
        public BaseViewModel ViewModel { get; private set; }

        public NavigationEventArgs(BaseViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }

}
