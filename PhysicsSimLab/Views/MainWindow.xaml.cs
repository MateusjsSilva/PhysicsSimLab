using System.Windows;
using PhysicsSimLab.Converters;
using PhysicsSimLab.ViewModels;

namespace PhysicsSimLab.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            
            // Initialize converter values
            ScaleConverter.Scale = 1.0;
            ScaleConverter.Offset = 0.0;
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
        
        private void SimulationCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the scale converter when the canvas is loaded
            ScaleConverter.Scale = 1.0;
            ScaleConverter.Offset = 0.0;
        }
    }
}