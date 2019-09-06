using System.Windows.Controls;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The switcher
    /// </summary>
    public static class Switcher
    {
        /// <summary>
        /// The page switcher
        /// </summary>
        public static PageSwitcher pageSwitcher;

        /// <summary>
        /// Switches the specified new page.
        /// </summary>
        /// <param name="newPage">The new page.</param>
        public static void Switch(UserControl newPage) => pageSwitcher.Navigate(newPage);

        /// <summary>
        /// Switches the specified new page.
        /// </summary>
        /// <param name="newPage">The new page.</param>
        /// <param name="state">The state.</param>
        public static void Switch(UserControl newPage, object state) => pageSwitcher.Navigate(newPage, state);
    }
}
