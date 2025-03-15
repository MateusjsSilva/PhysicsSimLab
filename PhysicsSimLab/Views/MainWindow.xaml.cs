using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhysicsSimLab.Models;
using PhysicsSimLab.Services;
using static PhysicsSimLab.Helpers.MathHelper;
using System.Runtime.InteropServices;

namespace PhysicsSimLab.Views
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        // Adicionar estas APIs do Windows para maximizar corretamente
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const int SW_MAXIMIZE = 3;

        private List<BallData> balls = new List<BallData>();
        private int activeBallIndex = -1;
        private const int MAX_BALLS = 5;
        
        private readonly List<SolidColorBrush> ballColors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.Blue),
            new SolidColorBrush(Colors.Green),
            new SolidColorBrush(Colors.Orange),
            new SolidColorBrush(Colors.Purple)
        };
        
        private PhysicsService physicsService;
        private VisualizationService visualizationService;
        private SimulationService simulationService;
        
        private bool isDragging = false;
        private double cameraOffsetX = 0;
        private double cameraOffsetY = 0;
        
        // References to removed variables
        private Polyline? trajectoryLine;
        private List<Point> trajectory = new List<Point>();
        private Line? xAxisLine;
        private Line? yAxisLine;
        private List<UIElement> xAxisMarkings = new List<UIElement>();
        private List<UIElement> yAxisMarkings = new List<UIElement>();
        
        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Maximizar a janela respeitando a barra de tarefas
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                // Usando SystemParameters.WorkArea para respeitar a barra de tarefas
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Width;
                Height = workArea.Height;
                
                // Atualizar ícone para mostrar o estado "restaurar"
                if (MaximizeIcon != null)
                {
                    MaximizeIcon.Text = "\uE923";  // Ícone de restaurar
                }
                
                // Sinalizar para a UI que estamos "maximizados" em termos de comportamento
                WindowState = WindowState.Normal; // Usamos Normal, mas com as dimensões da área de trabalho
            }));

            SetupSimulationCanvas();
            
            physicsService = new PhysicsService();
            visualizationService = new VisualizationService(SimulationCanvas);
            simulationService = new SimulationService(physicsService, visualizationService, balls);
            
            simulationService.TimeUpdated += (time) => {
                if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
                {
                    UpdateInfoPanel(balls[activeBallIndex], time);
                    UpdateInfoPanelPosition(balls[activeBallIndex]);
                }
                
                if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
                {
                    UpdateCamera(balls[activeBallIndex]);
                }
            };
            
            simulationService.SimulationStopped += () => {
                StartButton.Content = "Reiniciar";
            };
            
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                visualizationService.CreateCoordinateSystem();
                AddNewBall();
                CenterCamera();
            }));
            
            SimulationCanvas.MouseWheel += SimulationCanvas_MouseWheel;
            
            Style scrollBarStyle = (Style)FindResource("ThinScrollBar");
            SimulationScroller.Resources[typeof(ScrollBar)] = scrollBarStyle;
            
            CenterCamera();
        }

        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateScaleAndLimits();
            
            if (visualizationService != null)
            {
                visualizationService.UpdateGroundRect(SimulationCanvas.Width);
            }
        }
        
        private void SetupSimulationCanvas()
        {
            if (SimulationCanvas == null || SimulationScroller == null)
            {
                MessageBox.Show("Erro ao inicializar a simulação. Elementos de UI não encontrados.");
                return;
            }
            
            SimulationCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
            SimulationCanvas.VerticalAlignment = VerticalAlignment.Stretch;
   
            double viewportWidth = SimulationScroller.ViewportWidth > 0 ? SimulationScroller.ViewportWidth : ActualWidth;
            double viewportHeight = SimulationScroller.ViewportHeight > 0 ? SimulationScroller.ViewportHeight : ActualHeight;
            
            SimulationCanvas.Width = Math.Max(viewportWidth * 3, 3000);
            SimulationCanvas.Height = Math.Max(viewportHeight * 2, 1000);
            
            double groundY = double.IsNaN(SimulationCanvas.Height) ? 500 : SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            if (!double.IsNaN(SimulationCanvas.Width) && !double.IsNaN(SimulationScroller.ViewportWidth))
            {
                SimulationScroller.ScrollToHorizontalOffset(0);
            }
            
            if (visualizationService != null)
            {
                visualizationService.GroundY = groundY;
            }
        }
        
        private void UpdateScaleAndLimits()
        {
            if (visualizationService == null) return;
            
            if (SimulationScroller.ActualWidth > 0 && SimulationScroller.ActualHeight > 0)
            {
                visualizationService.Scale = Math.Min(SimulationScroller.ActualWidth / 80, SimulationScroller.ActualHeight / 40);
            }
            
            if (double.IsNaN(SimulationCanvas.Height))
                return;
                
            visualizationService.GroundY = SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, visualizationService.GroundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            visualizationService.CreateCoordinateSystem();
            
            foreach (var ball in balls)
            {
                if (ball.Visual != null)
                {
                    visualizationService.UpdateBallPosition(ball);
                }
            }
        }
        
        private void ResetSimulation()
        {
            simulationService.Stop();
            StartButton.Content = "Iniciar";
            
            double g;
            if (!TryParseInvariant(GravityTextBox.Text, out g) || g <= 0)
                g = 9.81;
            physicsService.Gravity = g;
                
            double airResistance;
            if (!TryParseInvariant(AirResistanceTextBox.Text, out airResistance))
                airResistance = 0.01;
            physicsService.AirResistance = airResistance;
                
            double atritoHorizontal;
            if (!TryParseInvariant(FrictionTextBox.Text, out atritoHorizontal) || 
                atritoHorizontal < 0 || atritoHorizontal > 1)
                atritoHorizontal = 0.95;
            physicsService.FrictionCoefficient = atritoHorizontal;
            
            simulationService.Reset();
            
            ApplyUIParametersToBall();
            CenterCamera();
        }
        
        void ApplyUIParametersToBall()
        {
            if (activeBallIndex < 0 || activeBallIndex >= balls.Count)
                return;

            BallData ball = balls[activeBallIndex];

            double mass;
            if (!TryParseInvariant(MassTextBox.Text, out mass) || mass <= 0)
                mass = 1.0;
            ball.Mass = mass;

            double vx;
            if (!TryParseInvariant(VxTextBox.Text, out vx))
                vx = 6;
            ball.Vx = vx;
            ball.InitialVx = vx;

            double vy;
            if (!TryParseInvariant(VyTextBox.Text, out vy))
                vy = 15;
            ball.Vy = vy;
            ball.InitialVy = vy;

            double restitution;
            if (!TryParseInvariant(RestituicaoTextBox.Text, out restitution) ||
                restitution < 0 || restitution > 1)
                restitution = 0.7;
            ball.Restitution = restitution;

            double size;
            if (!TryParseInvariant(BallSizeTextBox.Text, out size) || size <= 0)
                size = 30;
            ball.Size = size;

            if (ball.Visual != null)
            {
                ball.Visual.Width = ball.Size;
                ball.Visual.Height = ball.Size;
            }

            visualizationService.UpdateBallPosition(ball);
            UpdateInfoPanel(ball, simulationService.CurrentTime);
            UpdateInfoPanelPosition(ball);
        }
        
        private void CenterCamera()
        {
            if (SimulationScroller == null) return;
            
            try {
                SimulationScroller.ScrollToHorizontalOffset(0);
                
                if (balls.Count > 0 && activeBallIndex >= 0 && activeBallIndex < balls.Count)
                {
                    BallData activeBall = balls[activeBallIndex];
                    if (activeBall.Visual != null)
                    {
                        double ballY = Canvas.GetTop(activeBall.Visual) + activeBall.Visual.Height / 2;
                        cameraOffsetY = Math.Max(0, ballY - SimulationScroller.ViewportHeight / 2);
                        SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
                    }
                }
                else
                {
                    double middleY = visualizationService.GroundY / 2;
                    SimulationScroller.ScrollToVerticalOffset(Math.Max(0, middleY - SimulationScroller.ViewportHeight / 2));
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error in CenterCamera: {ex.Message}");
            }
        }
        
        #region Eventos UI
        
        private void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            if (simulationService.IsRunning)
            {
                simulationService.Stop();
                StartButton.Content = "Continuar";
            }
            else
            {
                simulationService.Start();
                StartButton.Content = "Pausar";
            }
        }
        
        private void ResetButton_Click(object? sender, RoutedEventArgs e)
        {
            simulationService.Stop();
            StartButton.Content = "Iniciar";
            ResetSimulation();
        }
        
        private void SimulationCanvas_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (simulationService.IsRunning || activeBallIndex < 0 || activeBallIndex >= balls.Count) return;
            
            Point position = e.GetPosition(SimulationCanvas);
            isDragging = true;
            
            BallData activeBall = balls[activeBallIndex];
            
            activeBall.X = position.X / visualizationService.Scale;
            activeBall.Y = (visualizationService.GroundY - position.Y) / visualizationService.Scale;
            
            if (activeBall.Y < 0) activeBall.Y = 0;
            
            visualizationService.UpdateBallPosition(activeBall);
            UpdateInfoPanel(activeBall, simulationService.CurrentTime);
            UpdateInfoPanelPosition(activeBall);
        }
        
        private void SimulationCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isDragging || simulationService.IsRunning || activeBallIndex < 0 || activeBallIndex >= balls.Count) return;
            
            Point position = e.GetPosition(SimulationCanvas);
            BallData activeBall = balls[activeBallIndex];
            
            activeBall.X = position.X / visualizationService.Scale;
            activeBall.Y = (visualizationService.GroundY - position.Y) / visualizationService.Scale;
            
            if (activeBall.Y < 0) activeBall.Y = 0;
            
            visualizationService.UpdateBallPosition(activeBall);
            UpdateInfoPanel(activeBall, simulationService.CurrentTime);
            UpdateInfoPanelPosition(activeBall);
        }
        
        private void SimulationCanvas_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                UpdateInitialPosition(balls[activeBallIndex]);
            }
        }
        
        private void SimulationCanvas_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                return;
            }

            if (balls.Count == 0 || activeBallIndex < 0) return;

            e.Handled = true;

            Point mousePosition = e.GetPosition(SimulationCanvas);

            double worldX = mousePosition.X / visualizationService.Scale;
            double worldY = (visualizationService.GroundY - mousePosition.Y) / visualizationService.Scale;

            if (e.Delta > 0)
            {
                visualizationService.Scale *= 1.1;
            }
            else
            {
                visualizationService.Scale /= 1.1;
            }

            visualizationService.Scale = Math.Max(1, Math.Min(visualizationService.Scale, 50));

            foreach (var ball in balls)
            {
                visualizationService.UpdateBallPosition(ball);
            }

            UpdateGroundPosition();

            foreach (var ball in balls)
            {
                UpdateTrajectoryVisual(ball);
            }

            visualizationService.CreateCoordinateSystem();

            double newMouseX = worldX * visualizationService.Scale;
            double newMouseY = visualizationService.GroundY - worldY * visualizationService.Scale;

            SimulationScroller.ScrollToHorizontalOffset(SimulationScroller.HorizontalOffset + (newMouseX - mousePosition.X));
            SimulationScroller.ScrollToVerticalOffset(SimulationScroller.VerticalOffset + (newMouseY - mousePosition.Y));
        }

        private void UpdateTrajectoryVisual(BallData ball)
        {
            if (ball.Trajectory == null || ball.TrajectoryPoints == null || ball.TrajectoryPoints.Count == 0) 
                return;
            
            try {
                PointCollection newPoints = new PointCollection();
                foreach (Point point in ball.TrajectoryPoints)
                {
                    newPoints.Add(point);
                }
                
                ball.Trajectory.Points = newPoints;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error in UpdateTrajectoryVisual for a ball: {ex.Message}");
            }
        }

        #endregion
        
        private void UpdateCamera(BallData ball)
        {
            if (SimulationScroller == null || ball.Visual == null) return;

            double ballLeft = Canvas.GetLeft(ball.Visual) + ball.Visual.Width / 2;
            double ballTop = Canvas.GetTop(ball.Visual) + ball.Visual.Height / 2;

            double idealOffsetX = -SimulationScroller.ViewportWidth / 2;
            double targetOffsetX = Math.Max(0, idealOffsetX);
            
            if (ballLeft > targetOffsetX + SimulationScroller.ViewportWidth * 0.9)
            {
                targetOffsetX = ballLeft - SimulationScroller.ViewportWidth * 0.9;
            }

            cameraOffsetX = cameraOffsetX + (targetOffsetX - cameraOffsetX) * 0.1;
            cameraOffsetY = cameraOffsetY + (ballTop - SimulationScroller.ViewportHeight / 2 - cameraOffsetY) * 0.1;

            cameraOffsetX = Math.Max(0, Math.Min(cameraOffsetX, SimulationCanvas.Width - SimulationScroller.ViewportWidth));
            cameraOffsetY = Math.Max(0, Math.Min(cameraOffsetY, SimulationCanvas.Height - SimulationScroller.ViewportHeight));

            if (!double.IsNaN(cameraOffsetX) && !double.IsNaN(cameraOffsetY))
            {
                SimulationScroller.ScrollToHorizontalOffset(cameraOffsetX);
                SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
            }
        }
        
        private void UpdateInfoPanel(BallData ball, double time)
        {
            double velocidadeTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            double energiaCinetica = physicsService.CalculateKineticEnergy(ball);
            double energiaPotencial = physicsService.CalculatePotentialEnergy(ball);
            double energiaTotal = energiaCinetica + energiaPotencial;
            
            string ballName = "Bola ?";
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                ballName = $"Bola {activeBallIndex + 1}";
            }
            
            InfoTextBlock.FontSize = 14;
            InfoTextBlock.FontWeight = FontWeights.SemiBold;
            InfoTextBlock.Text = $"{ballName}\n" +
                                 $"Tempo: {time:F2}s\n" +
                                 $"Posição X: {ball.X:F1}m\n" +
                                 $"Altura: {ball.Y:F1}m\n" +
                                 $"Velocidade X: {ball.Vx:F1}m/s\n" +
                                 $"Velocidade Y: {ball.Vy:F1}m/s\n" +
                                 $"Massa: {ball.Mass:F1}kg\n" +
                                 $"Tamanho: {ball.Size:F0}px\n" +
                                 $"E. Cinética: {energiaCinetica:F1}J\n" +
                                 $"E. Potencial: {energiaPotencial:F1}J\n" +
                                 $"E. Total: {energiaTotal:F1}J";
        }
        
        private void UpdateInfoPanelPosition(BallData ball)
        {
            if (ball.Visual == null || InfoPanel == null) return;
            
            double ballLeft = Canvas.GetLeft(ball.Visual);
            double ballTop = Canvas.GetTop(ball.Visual);
            
            Canvas.SetLeft(InfoPanel, ballLeft + ball.Visual.Width + 10);
            Canvas.SetTop(InfoPanel, ballTop - InfoPanel.ActualHeight - 5);
        }
        
        private void UpdateTrajectoryVisual()
        {
            if (trajectoryLine == null || trajectory == null || trajectory.Count == 0) return;
            
            try {
                PointCollection newPoints = new PointCollection();
                foreach (Point worldPoint in trajectory)
                {
                    double canvasX = worldPoint.X;
                    double canvasY = worldPoint.Y;
                    newPoints.Add(new Point(canvasX, canvasY));
                }
                
                trajectoryLine.Points = newPoints;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error in UpdateTrajectoryVisual: {ex.Message}");
            }
        }
        
        private void UpdateGroundPosition()
        {
            visualizationService.GroundY = SimulationCanvas.Height - 50; 

            if (GroundLine != null)
            {
                GroundLine.Visibility = Visibility.Collapsed;
            }
            
            visualizationService.UpdateGroundRect(SimulationCanvas.Width);
        }
        
        private void SimulationScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count && !isDragging && !simulationService.IsRunning)
            {
                BallData activeBall = balls[activeBallIndex];
                visualizationService.UpdateBallPosition(activeBall);
                UpdateInfoPanel(activeBall, simulationService.CurrentTime);
                UpdateInfoPanelPosition(activeBall);
            }
        }
        
        private void ZoomSlider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            visualizationService.Scale = e.NewValue;
            UpdateTrajectoryVisual();
        }
        
        private void PhysicsParameter_Changed(object? sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (simulationService != null && simulationService.IsRunning)
            {
                return;
            }
            
            TextBox? textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;

                double tempValue;
                if (!string.IsNullOrEmpty(text) && !TryParseInvariant(text, out tempValue))
                {
                    textBox.Background = new SolidColorBrush(Colors.LightPink);
                    return;
                }
                else
                {
                    textBox.Background = new SolidColorBrush(Colors.White);
                }
            }
            
            ResetSimulation();
        }

        private void AddNewBall()
        {
            if (balls.Count >= MAX_BALLS)
            {
                MessageBox.Show($"Número máximo de bolas ({MAX_BALLS}) atingido.");
                return;
            }
            
            BallData newBall = new BallData
            {
                X = 2,
                Y = 10,
                Vx = 6,
                Vy = 15,
                Mass = 1.0,
                Restitution = 0.7,
                Size = 30,
                Color = ballColors[balls.Count % ballColors.Count],
                InitialX = 2,
                InitialY = 10,
                InitialVx = 6,
                InitialVy = 15
            };
            
            Ellipse ballVisual = new Ellipse
            {
                Width = newBall.Size,
                Height = newBall.Size,
                Fill = newBall.Color,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            
            SimulationCanvas.Children.Add(ballVisual);
            Canvas.SetZIndex(ballVisual, 100);
            
            Polyline trajectory = new Polyline
            {
                Stroke = newBall.Color,
                StrokeThickness = 2,
                Opacity = 0.6
            };
            
            SimulationCanvas.Children.Add(trajectory);
            Canvas.SetZIndex(trajectory, 50);
            
            newBall.Visual = ballVisual;
            newBall.Trajectory = trajectory;
            
            balls.Add(newBall);
            
            SelectBall(balls.Count - 1);
            
            visualizationService.UpdateBallPosition(newBall);
            UpdateInfoPanel(newBall, simulationService.CurrentTime);
            UpdateInfoPanelPosition(newBall);
            
            UpdateBallSelectionUI();
        }
        
        private void RemoveActiveBall()
        {
            if (activeBallIndex < 0 || activeBallIndex >= balls.Count)
                return;
                
            if (balls[activeBallIndex].Visual != null)
                SimulationCanvas.Children.Remove(balls[activeBallIndex].Visual);
                
            if (balls[activeBallIndex].Trajectory != null)
                SimulationCanvas.Children.Remove(balls[activeBallIndex].Trajectory);
                
            balls.RemoveAt(activeBallIndex);
            
            if (balls.Count > 0)
            {
                activeBallIndex = Math.Max(0, activeBallIndex - 1);
                SelectBall(activeBallIndex);
            }
            else
            {
                activeBallIndex = -1;
            }
            
            UpdateBallSelectionUI();
        }
        
        private void SelectBall(int index)
        {
            if (index < 0 || index >= balls.Count)
                return;
                
            activeBallIndex = index;
            
            for (int i = 0; i < balls.Count; i++)
            {
                if (balls[i].Visual != null)
                {
                    balls[i].Visual.StrokeThickness = (i == activeBallIndex) ? 3 : 1;
                }
            }
            
            if (balls[activeBallIndex].Visual != null)
            {
                MassTextBox.Text = balls[activeBallIndex].Mass.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                VxTextBox.Text = balls[activeBallIndex].Vx.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                VyTextBox.Text = balls[activeBallIndex].Vy.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                RestituicaoTextBox.Text = balls[activeBallIndex].Restitution.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                BallSizeTextBox.Text = balls[activeBallIndex].Size.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                
                UpdateInfoPanel(balls[activeBallIndex], simulationService?.CurrentTime ?? 0);
                UpdateInfoPanelPosition(balls[activeBallIndex]);
            }
        }
        
        private void UpdateBallSelectionUI()
        {
            BallSelector.Items.Clear();
            
            for (int i = 0; i < balls.Count; i++)
            {
                BallSelector.Items.Add($"Bola {i + 1}");
            }
            
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                BallSelector.SelectedIndex = activeBallIndex;
            }
            
            RemoveBallButton.IsEnabled = balls.Count > 0;
            AddBallButton.IsEnabled = balls.Count < MAX_BALLS;
        }

        private void AddBallButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewBall();
        }
        
        private void RemoveBallButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveActiveBall();
        }
        
        private void BallSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = BallSelector.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < balls.Count)
            {
                SelectBall(selectedIndex);
            }
        }

        private void UpdateInitialPosition(BallData ball)
        {
            ball.InitialX = ball.X;
            ball.InitialY = ball.Y;
            ball.InitialVx = ball.Vx;
            ball.InitialVy = ball.Vy;
        }

        private void MenuItemSimular_Click(object sender, RoutedEventArgs e)
        {
            StartButton_Click(sender, e);
        }

        private void MenuItemResetar_Click(object sender, RoutedEventArgs e)
        {
            ResetButton_Click(sender, e);
        }

        private void MenuItemAdicionarBola_Click(object sender, RoutedEventArgs e)
        {
            AddBallButton_Click(sender, e);
        }

        private void MenuItemRemoverBola_Click(object sender, RoutedEventArgs e)
        {
            RemoveBallButton_Click(sender, e);
        }

        private void MenuItemSair_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Se o clique não foi em um TextBox ou outro controle de edição
            if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is FrameworkElement fe && 
                !(fe is TextBox) && !(fe.Parent is TextBox))
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Width == SystemParameters.WorkArea.Width && Height == SystemParameters.WorkArea.Height)
            {
                // Restaurar para o tamanho normal (antes de maximizar)
                WindowState = WindowState.Normal;
                Left = (SystemParameters.PrimaryScreenWidth - 957) / 2;
                Top = (SystemParameters.PrimaryScreenHeight - 627) / 2;
                Width = 957;
                Height = 627;
                
                if (MaximizeIcon != null)
                {
                    MaximizeIcon.Text = "\uE739";  // Ícone de maximizar
                }
            }
            else
            {
                // Maximizar respeitando a barra de tarefas
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Width;
                Height = workArea.Height;
                
                if (MaximizeIcon != null)
                {
                    MaximizeIcon.Text = "\uE923";  // Ícone de restaurar
                }
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Atualizar o ícone com base nas dimensões reais em vez do WindowState
            if (MaximizeIcon != null)
            {
                bool isMaximized = Width == SystemParameters.WorkArea.Width && 
                                  Height == SystemParameters.WorkArea.Height;
                MaximizeIcon.Text = isMaximized ? "\uE923" : "\uE739";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}