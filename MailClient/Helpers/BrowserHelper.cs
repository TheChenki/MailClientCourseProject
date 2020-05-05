using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MailClient.Helpers
{
    class BrowserHelper
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserHelper),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser webBrowser = dependencyObject as WebBrowser;
            if (webBrowser != null)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream,Encoding.UTF8);
                writer.Write(e.NewValue as string);
                writer.Flush();
                stream.Position = 0;

                webBrowser.NavigateToStream(stream);
            }
                //webBrowser.NavigateToString(e.NewValue as string ?? "&nbsp;");
        }
    }
}
