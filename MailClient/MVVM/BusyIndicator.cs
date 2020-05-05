using System;

namespace MailClient.MVVM
{
    public class BusyIndicator : IDisposable
    {
        public static event EventHandler Busy;
        public static event EventHandler Free;

        public BusyIndicator()
        {
            Busy.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Free.Invoke(this, EventArgs.Empty);
        }
    }
}
