using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ComicaggApp.Pages
{
    public sealed partial class ComicStripControl : UserControl
    {
        public ComicStripControl()
        {
            this.InitializeComponent();
        }

        private void StripImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            Image img = (Image)sender;
            double imgWidth = img.ActualWidth;
            double viewportWidth = (double)img.Parent.GetValue(StackPanel.ActualWidthProperty);
            if (imgWidth > viewportWidth)
            {
                img.Stretch = Stretch.Uniform;
            }
            StripProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
    }
}
