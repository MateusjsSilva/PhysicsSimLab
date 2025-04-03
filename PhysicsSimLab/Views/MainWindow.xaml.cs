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

        private void MenuItemSimular_Click(object sender, RoutedEventArgs e)
        {
            // Toggle simulation start/pause logic
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                if (viewModel.IsSimulationRunning)
                    viewModel.StopCommand.Execute(null);
                else
                    viewModel.StartCommand.Execute(null);
            }
        }

        private void MenuItemResetar_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.ResetCommand.Execute(null);
        }

        private void MenuItemAdicionarBola_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.AddBallCommand.Execute(null);
        }

        private void MenuItemRemoverBola_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.RemoveBallCommand.Execute(null);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeIcon.Text = "\uE739"; // Restore icon
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeIcon.Text = "\uE923"; // Maximize icon
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}