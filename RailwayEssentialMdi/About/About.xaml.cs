using System.Windows;

namespace RailwayEssentialMdi.About
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void CmdOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
