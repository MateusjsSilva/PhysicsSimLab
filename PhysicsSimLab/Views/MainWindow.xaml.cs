using System.Windows;
using PhysicsSimLab.ViewModels;

namespace PhysicsSimLab.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Physics Simulation Lab\nA simulation tool for physics experiments.",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
