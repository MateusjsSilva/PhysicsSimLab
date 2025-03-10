using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PhysicsSimLab
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        private double g = 9.81;
        
        private class BallData
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Vx { get; set; }
            public double Vy { get; set; }
            public double Mass { get; set; }
            public double Restitution { get; set; }
            public double Size { get; set; }
            public SolidColorBrush Color { get; set; }
            public Ellipse? Visual { get; set; }
            public Polyline? Trajectory { get; set; }
            public List<Point> TrajectoryPoints { get; set; } = new List<Point>();
            
            public BallData Clone()
            {
                return new BallData
                {
                    X = this.X,
                    Y = this.Y,
                    Vx = this.Vx,
                    Vy = this.Vy,
                    Mass = this.Mass,
                    Restitution = this.Restitution,
                    Size = this.Size,
                    Color = new SolidColorBrush(this.Color.Color)
                };
            }
        }
        
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
        
        private double time = 0;
        private readonly double dt = 0.016;
        private double atritoHorizontal = 0.95;
        private double airResistance = 0.01;
        
        private Ellipse? ball;
        private readonly List<Point> trajectory = new List<Point>();
        private Polyline? trajectoryLine;
        private DispatcherTimer timer;
        private Line? yAxisLine;
        private Line? xAxisLine;
        private readonly List<UIElement> yAxisMarkings = new List<UIElement>(); 
        private readonly List<UIElement> xAxisMarkings = new List<UIElement>();
        
        private double scale = 10;
        private double groundY;
        
        private bool isDragging = false;
        private bool simulationStarted = false;
        
        private double cameraOffsetX = 0;
        private double cameraOffsetY = 0;
        
        public MainWindow()
        {
            InitializeComponent();
            
            timer = new DispatcherTimer();
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(dt);
            
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            
            WindowState = WindowState.Maximized;
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            if (balls.Count == 0)
            {
                timer.Stop();
                return;
            }
            
            bool allStopped = true;
            
            foreach (var ball in balls)
            {
                if (ball.Visual == null || ball.Trajectory == null) 
                    continue;
                
                UpdatePhysics(ball);
                
                UpdateBallPosition(ball);
                
                AddTrajectoryPoint(ball);
                
                if (Math.Abs(ball.Vy) >= 0.1 || Math.Abs(ball.Vx) >= 0.1 || ball.Y > 0.01)
                {
                    allStopped = false;
                }
            }
            
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                UpdateInfoPanel(balls[activeBallIndex]);
                UpdateInfoPanelPosition(balls[activeBallIndex]);
            }
            
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                UpdateCamera(balls[activeBallIndex]);
            }
            
            time += dt;
            
            if (allStopped || time >= 30)
            {
                timer.Stop();
                StartButton.Content = "Reiniciar";
            }
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            SetupSimulationCanvas();
            
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                CreateCoordinateSystem();
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
        }
        
        private void SetupSimulationCanvas()
        {
            if (SimulationCanvas == null || SimulationScroller == null)
            {
                MessageBox.Show("Erro ao inicializar a simulação. Elementos de UI não encontrados.");
                return;
            }

            SimulationCanvas.Width = Math.Max(ActualWidth * 3, 3000);
            SimulationCanvas.Height = Math.Max(ActualHeight * 2, 1000);
            
            groundY = double.IsNaN(SimulationCanvas.Height) ? 500 : SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            if (!double.IsNaN(SimulationCanvas.Width) && !double.IsNaN(SimulationScroller.ViewportWidth))
            {
                SimulationScroller.ScrollToHorizontalOffset(0);
            }
        }
        
        private void CreateCoordinateSystem()
        {
            if (double.IsNaN(groundY) || double.IsInfinity(groundY))
            {
                groundY = SimulationCanvas.ActualHeight > 50 ? SimulationCanvas.ActualHeight - 50 : 500;
            }

            ClearCoordinateSystem();
            
            try
            {
                yAxisLine = new Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = groundY,
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 3 
                };
                
                SimulationCanvas.Children.Add(yAxisLine);
                Canvas.SetZIndex(yAxisLine, 90); 
                Canvas.SetLeft(yAxisLine, 0); 

                xAxisLine = new Line
                {
                    X1 = 0,
                    X2 = SimulationCanvas.Width,
                    Y1 = groundY,
                    Y2 = groundY,
                    Stroke = Brushes.DarkBlue, 
                    StrokeThickness = 3 
                };
                
                SimulationCanvas.Children.Add(xAxisLine);
                Canvas.SetZIndex(xAxisLine, 90);
                
                for (int i = 5; i <= 100; i += 5)
                {
                    double xPos = i * scale;
                    
                    Line tick = new Line
                    {
                        X1 = xPos,
                        X2 = xPos,
                        Y1 = groundY - 5,
                        Y2 = groundY + 5,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    
                    TextBlock tickLabel = new TextBlock
                    {
                        Text = i.ToString(),
                        FontSize = 10
                    };
                    
                    Canvas.SetLeft(tickLabel, xPos - 8);
                    Canvas.SetTop(tickLabel, groundY + 8);
                    
                    SimulationCanvas.Children.Add(tick);
                    SimulationCanvas.Children.Add(tickLabel);
                    Canvas.SetZIndex(tick, 50);
                    Canvas.SetZIndex(tickLabel, 50);
                    
                    xAxisMarkings.Add(tick);
                    xAxisMarkings.Add(tickLabel);
                }
                
                TextBlock originLabel = new TextBlock
                {
                    Text = "0",
                    FontSize = 10
                };
                Canvas.SetLeft(originLabel, -15);
                Canvas.SetTop(originLabel, groundY + 8);
                SimulationCanvas.Children.Add(originLabel);
                Canvas.SetZIndex(originLabel, 50);
                xAxisMarkings.Add(originLabel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating coordinate system: {ex.Message}");
            }
        }
        
        private void ClearCoordinateSystem()
        {
            foreach (var mark in yAxisMarkings)
            {
                SimulationCanvas.Children.Remove(mark);
            }
            yAxisMarkings.Clear();
            
            if (yAxisLine != null)
            {
                SimulationCanvas.Children.Remove(yAxisLine);
            }
            
            foreach (var mark in xAxisMarkings)
            {
                SimulationCanvas.Children.Remove(mark);
            }
            xAxisMarkings.Clear();
        }
        
        private void UpdateScaleAndLimits()
        {
            if (SimulationScroller.ActualWidth > 0 && SimulationScroller.ActualHeight > 0)
            {
                scale = Math.Min(SimulationScroller.ActualWidth / 80, SimulationScroller.ActualHeight / 40);
            }
            
            if (double.IsNaN(SimulationCanvas.Height))
                return;
                
            groundY = SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            UpdateCoordinateSystem();
            
            foreach (var ball in balls)
            {
                if (ball.Visual != null)
                {
                    UpdateBallPosition(ball);
                }
            }
        }
        
        private void UpdateCoordinateSystem()
        {
            ClearCoordinateSystem();
            CreateCoordinateSystem();
        }
        
        private void InitializeSimulation()
        {
            if (trajectoryLine != null)
                SimulationCanvas.Children.Remove(trajectoryLine);
                
            if (ball != null)
                SimulationCanvas.Children.Remove(ball);
                
            trajectory.Clear();
            
            if (!TryParseInvariant(GravityTextBox.Text, out g) || g <= 0)
                g = 9.81;
                
            if (!TryParseInvariant(AirResistanceTextBox.Text, out airResistance))
                airResistance = 0.01;
                
            if (!TryParseInvariant(FrictionTextBox.Text, out atritoHorizontal) || 
                atritoHorizontal < 0 || atritoHorizontal > 1)
                atritoHorizontal = 0.95;
        }
        
        private void ResetSimulation()
        {
            timer.Stop();
            StartButton.Content = "Iniciar";
            time = 0;
            simulationStarted = false;
            
            if (!TryParseInvariant(GravityTextBox.Text, out g) || g <= 0)
                g = 9.81;
                
            if (!TryParseInvariant(AirResistanceTextBox.Text, out airResistance))
                airResistance = 0.01;
                
            if (!TryParseInvariant(FrictionTextBox.Text, out atritoHorizontal) || 
                atritoHorizontal < 0 || atritoHorizontal > 1)
                atritoHorizontal = 0.95;
            
            foreach (var ball in balls)
            {
                if (ball.Trajectory != null)
                {
                    ball.Trajectory.Points.Clear();
                }
            }
            
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

            double vy;
            if (!TryParseInvariant(VyTextBox.Text, out vy))
                vy = 15;
            ball.Vy = vy;

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

            UpdateBallPosition(ball);
            UpdateInfoPanel(ball);
            UpdateInfoPanelPosition(ball);
        }
        
        private void CenterCamera()
        {
            if (SimulationScroller == null) return;
            
            try {
                SimulationScroller.ScrollToHorizontalOffset(0);
                
                if (ball != null)
                {
                    double ballY = Canvas.GetTop(ball) + ball.Height / 2;
                    cameraOffsetY = Math.Max(0, ballY - SimulationScroller.ViewportHeight / 2);
                    SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
                }
                else
                {
                    double middleY = groundY / 2;
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
            if (timer.IsEnabled)
            {
                timer.Stop();
                StartButton.Content = "Continuar";
            }
            else
            {
                timer.Start();
                StartButton.Content = "Pausar";
                
                if (!simulationStarted)
                {
                    simulationStarted = true;
                }
            }
        }
        
        private void ResetButton_Click(object? sender, RoutedEventArgs e)
        {
            timer.Stop();
            StartButton.Content = "Iniciar";
            ResetSimulation();
        }
        
        private void SimulationCanvas_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (simulationStarted || activeBallIndex < 0 || activeBallIndex >= balls.Count) return;
            
            Point position = e.GetPosition(SimulationCanvas);
            isDragging = true;
            
            BallData activeBall = balls[activeBallIndex];
            
            activeBall.X = position.X / scale;
            activeBall.Y = (groundY - position.Y) / scale;
            
            if (activeBall.Y < 0) activeBall.Y = 0;
            
            UpdateBallPosition(activeBall);
            UpdateInfoPanel(activeBall);
            UpdateInfoPanelPosition(activeBall);
        }
        
        private void SimulationCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isDragging || simulationStarted || activeBallIndex < 0 || activeBallIndex >= balls.Count) return;
            
            Point position = e.GetPosition(SimulationCanvas);
            BallData activeBall = balls[activeBallIndex];
            
            activeBall.X = position.X / scale;
            activeBall.Y = (groundY - position.Y) / scale;
            
            if (activeBall.Y < 0) activeBall.Y = 0;
            
            UpdateBallPosition(activeBall);
            UpdateInfoPanel(activeBall);
            UpdateInfoPanelPosition(activeBall);
        }
        
        private void SimulationCanvas_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            isDragging = false;
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

            double worldX = mousePosition.X / scale;
            double worldY = (groundY - mousePosition.Y) / scale;

            if (e.Delta > 0)
            {
                scale *= 1.1;
            }
            else
            {
                scale /= 1.1;
            }

            scale = Math.Max(1, Math.Min(scale, 50));

            foreach (var ball in balls)
            {
                UpdateBallPosition(ball);
            }

            UpdateGroundPosition();

            foreach (var ball in balls)
            {
                UpdateTrajectoryVisual(ball);
            }

            UpdateCoordinateSystem();

            double newMouseX = worldX * scale;
            double newMouseY = groundY - worldY * scale;

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
        
        private void UpdatePhysics(BallData ball)
        {
            ball.X += ball.Vx * dt;
            ball.Y += ball.Vy * dt - 0.5 * g * dt * dt;
            
            if (ball.X < 0)
            {
                ball.X = 0;
                ball.Vx = -ball.Vx * ball.Restitution;
            }
            
            double vTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            if (vTotal > 0)
            {
                double dragForceMagnitude = airResistance * vTotal * vTotal;
                double dragForceX = -dragForceMagnitude * ball.Vx / vTotal;
                double dragForceY = -dragForceMagnitude * ball.Vy / vTotal;
                
                ball.Vx += dragForceX * dt;
                ball.Vy += dragForceY * dt;
            }
            
            ball.Vy -= g * dt;
            
            if (ball.Y <= 0.01 && ball.Vy < 0 && ball.Visual != null)
            {
                ball.Y = 0.01;
                
                double impactVelocity = Math.Abs(ball.Vy);
                
                ball.Vy = -ball.Vy * ball.Restitution;
                
                ball.Vx *= atritoHorizontal;
                
                if (impactVelocity > 2.0)
                {
                    try
                    {
                        double squashFactor = Math.Min(0.6 + (impactVelocity / 50), 0.8);
                        double stretchFactor = 1.0 + (1.0 - squashFactor);
                        
                        ScaleTransform scaleTransform = new ScaleTransform(stretchFactor, squashFactor);
                        ball.Visual.RenderTransform = scaleTransform;
                        
                        DispatcherTimer resetScale = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };
                        
                        resetScale.Tick += (s, e) =>
                        {
                            if (ball.Visual != null)
                            {
                                ball.Visual.RenderTransform = null;
                            }
                            resetScale.Stop();
                        };
                        
                        resetScale.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in collision handling: {ex.Message}");
                        if (ball.Visual != null)
                            ball.Visual.RenderTransform = null;
                    }
                }
                
                if (Math.Abs(ball.Vy) < 0.3)
                    ball.Vy = 0;
                    
                if (Math.Abs(ball.Vx) < 0.3)
                    ball.Vx = 0;
            }
        }
        
        private void UpdateBallPosition(BallData ball)
        {
            if (ball.Visual == null) return;
            
            double canvasX = ball.X * scale;
            double canvasY = groundY - ball.Y * scale;
            
            Canvas.SetLeft(ball.Visual, canvasX - ball.Visual.Width / 2);
            Canvas.SetTop(ball.Visual, canvasY - ball.Visual.Height / 2);
        }

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

        private void AddTrajectoryPoint(BallData ball)
        {
            if (ball.Trajectory == null) return;
            
            double canvasX = ball.X * scale;
            double canvasY = groundY - ball.Y * scale;
            
            ball.TrajectoryPoints.Add(new Point(canvasX, canvasY));
            
            try {
                ball.Trajectory.Points = new PointCollection(ball.TrajectoryPoints);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error updating trajectory points: {ex.Message}");
            }
        }
        
        private void UpdateInfoPanel(BallData ball)
        {
            double velocidadeTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            double energiaCinetica = 0.5 * ball.Mass * velocidadeTotal * velocidadeTotal;
            double energiaPotencial = ball.Mass * g * ball.Y;
            double energiaTotal = energiaCinetica + energiaPotencial;
            
            InfoTextBlock.FontSize = 14;
            InfoTextBlock.FontWeight = FontWeights.SemiBold;
            InfoTextBlock.Text = $"Tempo: {time:F2}s\n" +
                                 $"Posição X: {ball.X:F1}m\n" +
                                 $"Altura: {ball.Y:F1}m\n" +
                                 $"Velocidade X: {ball.Vx:F1}m/s\n" +
                                 $"Velocidade Y: {ball.Vy:F1}m/s\n" +
                                 $"Massa: {ball.Mass:F1}kg\n" +
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
            groundY = SimulationCanvas.Height - 50; 
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
        }
        
        private void SimulationScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count && !isDragging && !timer.IsEnabled)
            {
                BallData activeBall = balls[activeBallIndex];
                UpdateBallPosition(activeBall);
                UpdateInfoPanel(activeBall);
                UpdateInfoPanelPosition(activeBall);
            }
        }
        
        private void ZoomSlider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scale = e.NewValue;
            UpdateTrajectoryVisual();
        }
        
        private void PhysicsParameter_Changed(object? sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (timer.IsEnabled)
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

        private bool TryParseInvariant(string text, out double result)
        {
            string normalizedText = text.Replace(',', '.');
            
            return double.TryParse(normalizedText, 
                                  System.Globalization.NumberStyles.Any, 
                                  System.Globalization.CultureInfo.InvariantCulture, 
                                  out result);
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
                Color = ballColors[balls.Count % ballColors.Count]
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
            
            UpdateBallPosition(newBall);
            UpdateInfoPanel(newBall);
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
                
                UpdateInfoPanel(balls[activeBallIndex]);
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
    }
}