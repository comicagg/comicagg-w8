using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

namespace ComicaggApp.Pages
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ReadComics : ComicaggApp.Common.LayoutAwarePage
    {
        public ReadComics()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadData();
        }

        public async void LoadData()
        {
            //Reset layout
            ClearPage();
            ShowOverlay();
            OverlayText.Text = Application.Current.Resources["LoadingText"] as string;

            //Load the unread comics from the server
            String ret = await WebHelper.Request("/api/unread/withstrips/", WebHelper.Methods.GET, null, true);

            if (ret == null)
            {
                //We didnt get a proper response
                OverlayText.Text = Application.Current.Resources["ErrorFetchingText"] as string;
                return;
            }


            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ret);
            XmlNodeList unreadComics = doc.GetElementsByTagName("comic");

            if (unreadComics.Count > 0)
            {
                //Create the button to choose the comic from the left
                Style s = Application.Current.Resources["ButtonReadComicsList"] as Style;
                foreach (XmlElement comic in unreadComics)
                {
                    Button b = new Button();
                    b.Style = s;
                    b.Content = String.Format("{0} ({1})", comic.Attributes.GetNamedItem("name").NodeValue, comic.GetElementsByTagName("strip").Count);
                    b.Tag = comic;
                    b.Click += ButtonComicList_Click;
                    StackComics.Children.Add(b);
                }
                //Hide the overlay, get everyhting ready to read the comics and load the first comic
                HideOverlay();
                TotalUnreads.Text = String.Format(Application.Current.Resources["TotalUnreadsText"] as string, unreadComics.Count);
                TotalUnreads.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //HowToPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ButtonComicList_Click(StackComics.Children.ElementAt(0), null);

            }
            else
            {
                //No unread comics, just change the text in the overlay
                OverlayText.Text = Application.Current.Resources["NoUnreadComicsText"] as string;
            }
        }

        private void ClearPage()
        {
            StackComics.Children.Clear();
            StackStrips.Children.Clear();
            ButtonMarkRead.Opacity = 0;
            //HowToPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            TotalUnreads.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void HideOverlay()
        {
            Overlay.SetValue(Canvas.ZIndexProperty, -1);
            Overlay.Opacity = 0;
        }

        private void ShowOverlay()
        {
            Overlay.SetValue(Canvas.ZIndexProperty, 1);
            Overlay.Opacity = 1;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        int CurrentComicIndex = -1;
        int CurrentComicId = -1;

        private void ButtonComicList_Click(object sender, RoutedEventArgs e)
        {
            //HowToPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //Preparations for later
            Button origin = (Button) sender;
            CurrentComicIndex = StackComics.Children.IndexOf(origin);
            XmlElement node = (XmlElement)((Button)sender).Tag;
            CurrentComicId = int.Parse((string)node.Attributes.GetNamedItem("id").NodeValue);
            StackStrips.Children.Clear();

            //Read the unread strips for this comic and show each strip
            XmlNodeList strips = node.GetElementsByTagName("strip");

            foreach (XmlElement strip in strips)
            {
                //Create the strip and set up the data for the binding
                ComicStripControl cs = new ComicStripControl();
                ComicStripData uc = new ComicStripData();
                uc.Url = new Uri((string)strip.Attributes.GetNamedItem("imageurl").NodeValue);
                uc.AltText = System.Net.WebUtility.HtmlDecode((string)strip.Attributes.GetNamedItem("imagetext").NodeValue);
                uc.Date = (string)strip.Attributes.GetNamedItem("date").NodeValue;
                cs.DataContext = uc;
                StackStrips.Children.Add(cs);
            }
            ButtonMarkRead.Opacity = 1;
        }

        private async void MarkReadAndMoveNext(object sender, RoutedEventArgs e)
        {
            if (CurrentComicIndex == -1 || CurrentComicId == -1) return;
            //Mark as read in the server
            Dictionary<string, string> kv = new Dictionary<string, string>();
            kv["vote"] = "0";
            String ret = await WebHelper.Request(String.Format("/api/comic/{0}/", CurrentComicId), WebHelper.Methods.POST, kv, true);

            if (ret == null)
            {
                //Something bad happened
                //TODO
            }
            else
            {
                //Remove the button from the menu and show the next one if available.
                StackComics.Children.RemoveAt(CurrentComicIndex);
                if (StackComics.Children.Count > 0)
                {
                    if (CurrentComicIndex >= StackComics.Children.Count)
                        CurrentComicIndex = StackComics.Children.Count - 1;
                    //Load next comic
                    ButtonComicList_Click(StackComics.Children.ElementAt(CurrentComicIndex), null);
                }
                else
                {
                    //There are no more unread comics, clear everything and show message
                    ClearPage();
                    ShowOverlay();
                    OverlayText.Text = Application.Current.Resources["NoUnreadComicsText"] as string;
                }
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ButtonHome_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(WelcomeUser));
        }
    }

    class ComicStripData
    {
        Uri _url;
        public Uri Url { get { return _url; } set { _url = value; } }

        string _alt;
        public string AltText { get { return _alt; } set { _alt = value; } }

        DateTime _date;
        public string Date { get { return _date.ToString("d MMMM"); } set { _date = DateTime.Parse(value); } }
    }
}
