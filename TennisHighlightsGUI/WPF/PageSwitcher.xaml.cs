using System;
using System.Windows;
using System.Windows.Controls;
using TennisHighlightsGUI.WPF;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PageSwitcher : Window
    {
        private MainWindow _main;

        public PageSwitcher()
        {
            var viewModel = new PageSwitchViewModel(640); 

            DataContext = viewModel;

            InitializeComponent();
            Switcher.pageSwitcher = this;

            var mainWindow = new MainWindow();

            //This will prevents main windows's resources from being freed. Shouldn't be an issue, but good to note.
            this.Closing += mainWindow.MainWindow_Closing;
            
            Switcher.Switch(mainWindow);
        }       

        /// <summary>
        /// Navigates the specified next page.
        /// </summary>
        /// <param name="nextPage">The next page.</param>
        public void Navigate(UserControl nextPage) => Content = nextPage;

        /// <summary>
        /// Navigates the specified next page.
        /// </summary>
        /// <param name="nextPage">The next page.</param>
        /// <param name="state">The state.</param>
        /// <exception cref="ArgumentException">NextPage is not ISwitchable! " + nextPage.Name.ToString()</exception>
        public void Navigate(UserControl nextPage, object state)
        {
            this.Content = nextPage;

            if (nextPage is ISwitchable s)
            {
                s.UtilizeState(state);
            }
            else
            {
                throw new ArgumentException("NextPage is not ISwitchable! " + nextPage.Name.ToString());
            }
        }
    }
}
