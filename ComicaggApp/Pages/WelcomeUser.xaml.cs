using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ComicaggApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WelcomeUser : Page
    {
        public WelcomeUser()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ShowOverlay();
            //Do the initial request to get the username and the number of unread comics.
            String ret = await WebHelper.DoRequest("/api/user/", WebHelper.Methods.GET, null, true);
            if (ret == null)
            {
                //We didnt get a proper response
                //OverlayText.Text = Application.Current.Resources["ErrorFetchingText"] as string;
                HideOverlay();
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ret);
            string username = (string)doc.FirstChild.NextSibling.Attributes.GetNamedItem("username").NodeValue;
            int unreadCount = int.Parse((string)doc.FirstChild.NextSibling.Attributes.GetNamedItem("totalunreads").NodeValue);

            WelcomeTitle.Text = String.Format(WelcomeTitle.Text, username);

            if (unreadCount == 0)
            {
                ReadComicsButton.Content = Application.Current.Resources["NoNewComicsButtonText"] as string;
            }
            else
            {
                string s = Application.Current.Resources["NewComicsButtonText"] as string;
                ReadComicsButton.Content = String.Format(s, unreadCount);
            }
            //Show the welcome panel, hide the overlay
            HideOverlay();
        }

        private void HideOverlay()
        {
            WelcomePanel.Opacity = 1;
            Overlay.SetValue(Canvas.ZIndexProperty, -1);
            Overlay.Opacity = 0;
        }

        private void ShowOverlay()
        {
            WelcomePanel.Opacity = 0;
            Overlay.SetValue(Canvas.ZIndexProperty, 1);
            Overlay.Opacity = 0.3;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ReadComics));
        }

        private void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {
            WebHelper.Logout();
            this.Frame.Navigate(typeof(WelcomeAnon));
        }
    }
}
