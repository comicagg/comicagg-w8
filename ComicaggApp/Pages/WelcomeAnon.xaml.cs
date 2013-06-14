using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
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
    public sealed partial class WelcomeAnon : Page
    {
        public WelcomeAnon()
        {
            this.InitializeComponent();
        }

        private async void ButtonStartLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginResult.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ButtonStartLogin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            loginProgress.IsActive = true;
            await WebHelper.Login();
            loginProgress.IsActive = false;
            if (WebHelper.HaveAccessToken())
            {
                this.Frame.Navigate(typeof(WelcomeUser));
            }
            else
            {
                LoginResult.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ButtonStartLogin.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }
    }
}
