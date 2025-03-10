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
        // Constantes físicas
        private double g = 9.81;  // Aceleração da gravidade (m/s²)
        
        // Classe para armazenar dados de cada bola
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
        
        // Lista de bolas e índice da bola ativa
        private List<BallData> balls = new List<BallData>();
        private int activeBallIndex = -1;
        private const int MAX_BALLS = 5;
        
        // Lista de cores para bolas diferentes
        private readonly List<SolidColorBrush> ballColors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.Blue),
            new SolidColorBrush(Colors.Green),
            new SolidColorBrush(Colors.Orange),
            new SolidColorBrush(Colors.Purple)
        };
        
        // Variáveis de simulação
        private double time = 0;   // Tempo de simulação
        private readonly double dt = 0.016; // ~60 FPS
        
        // Configurações compartilhadas
        private double atritoHorizontal = 0.95;
        private double airResistance = 0.01;
        
        // Elementos visuais
        private Ellipse? ball;  // Mark as nullable
        private readonly List<Point> trajectory = new List<Point>();
        private Polyline? trajectoryLine;  // Mark as nullable
        private DispatcherTimer timer;
        private Line? yAxisLine;  // Representação visual do eixo Y
        private Line? xAxisLine;  // Representação visual do eixo X
        private readonly List<UIElement> yAxisMarkings = new List<UIElement>(); // Marcações no eixo Y
        private readonly List<UIElement> xAxisMarkings = new List<UIElement>(); // Marcações no eixo X
        
        // Mapeamento do mundo para canvas
        private double scale = 10; // pixels por metro
        private double groundY;    // posição Y do solo em pixels
        
        // Controle de mouse
        private bool isDragging = false;
        private bool simulationStarted = false;
        
        // Camera follow
        private double cameraOffsetX = 0;
        private double cameraOffsetY = 0;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Inicializa o timer
            timer = new DispatcherTimer();
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(dt);
            
            // Inicialização após carregamento
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            
            // Começar em tela cheia
            WindowState = WindowState.Maximized;
        }

        // Update method signature to match EventHandler delegate
        private void TimerTick(object? sender, EventArgs e)
        {
            if (balls.Count == 0)
            {
                timer.Stop();
                return;
            }
            
            bool allStopped = true;
            
            // Atualizar cada bola
            foreach (var ball in balls)
            {
                if (ball.Visual == null || ball.Trajectory == null) 
                    continue;
                
                // Atualizar a física desta bola
                UpdatePhysics(ball);
                
                // Atualizar a posição visual
                UpdateBallPosition(ball);
                
                // Adicionar ponto à trajetória
                AddTrajectoryPoint(ball);
                
                // Verificar se a bola ainda está em movimento
                if (Math.Abs(ball.Vy) >= 0.1 || Math.Abs(ball.Vx) >= 0.1 || ball.Y > 0.01)
                {
                    allStopped = false;
                }
            }
            
            // Atualizar o painel de informações para a bola ativa
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                UpdateInfoPanel(balls[activeBallIndex]);
                UpdateInfoPanelPosition(balls[activeBallIndex]);
            }
            
            // Implementa a camera para seguir a bola ativa
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                UpdateCamera(balls[activeBallIndex]);
            }
            
            // Incrementa o tempo
            time += dt;
            
            // Verificar se a simulação deve parar
            if (allStopped || time >= 30)
            {
                timer.Stop();
                StartButton.Content = "Reiniciar";
            }
        }

        // Make sure all methods that can receive null parameters have proper signatures
        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Configura dimensões iniciais
            SetupSimulationCanvas();
            
            // Adicionar o sistema de coordenadas completo - delay slightly to ensure canvas is ready
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                CreateCoordinateSystem();
                
                // Adicionar a primeira bola
                AddNewBall();
                
                // Centralizar a câmera
                CenterCamera();
            }));
            
            // Registrar o evento de roda do mouse para zoom
            SimulationCanvas.MouseWheel += SimulationCanvas_MouseWheel;
            
            // Apply the thin scrollbar style
            Style scrollBarStyle = (Style)FindResource("ThinScrollBar");
            SimulationScroller.Resources[typeof(ScrollBar)] = scrollBarStyle;
            
            // Centralizar a câmera para ver o eixo Y
            CenterCamera();
        }

        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Atualiza dimensões quando a janela mudar de tamanho
            UpdateScaleAndLimits();
        }
        
        private void SetupSimulationCanvas()
        {
            // Certifica que o Canvas e ScrollViewer estão propriamente configurados
            if (SimulationCanvas == null || SimulationScroller == null)
            {
                MessageBox.Show("Erro ao inicializar a simulação. Elementos de UI não encontrados.");
                return;
            }

            // Use the full available space for the simulation, with extra width for movement
            SimulationCanvas.Width = Math.Max(ActualWidth * 3, 3000);
            SimulationCanvas.Height = Math.Max(ActualHeight * 2, 1000);
            
            // Position ground at the bottom with validation
            groundY = double.IsNaN(SimulationCanvas.Height) ? 500 : SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            // Ajusta o scroll para centralizar o eixo Y
            if (!double.IsNaN(SimulationCanvas.Width) && !double.IsNaN(SimulationScroller.ViewportWidth))
            {
                SimulationScroller.ScrollToHorizontalOffset(0); // Start at left edge to see y-axis
            }
        }
        
        // Método para criar o sistema de coordenadas completo
        private void CreateCoordinateSystem()
        {
            // Verificar se groundY é válido
            if (double.IsNaN(groundY) || double.IsInfinity(groundY))
            {
                // Definir um valor padrão seguro se groundY for inválido
                groundY = SimulationCanvas.ActualHeight > 50 ? SimulationCanvas.ActualHeight - 50 : 500;
            }

            // Limpar sistema de coordenadas existente
            ClearCoordinateSystem();
            
            try
            {
                // Criar linha do eixo Y em x=0 com mais visibilidade
                yAxisLine = new Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = groundY,
                    Stroke = Brushes.DarkBlue, // Cor mais visível
                    StrokeThickness = 3 // Linha mais grossa
                };
                
                SimulationCanvas.Children.Add(yAxisLine);
                Canvas.SetZIndex(yAxisLine, 90); // Garantir que fique bem visível
                Canvas.SetLeft(yAxisLine, 0); // Certificar que está em X=0
                
                // Debug info
                Console.WriteLine($"Y-Axis created: X1={yAxisLine.X1}, Y1={yAxisLine.Y1}, X2={yAxisLine.X2}, Y2={yAxisLine.Y2}");
                
                // Criar linha do eixo X em y=groundY (solo)
                xAxisLine = new Line
                {
                    X1 = 0,
                    X2 = SimulationCanvas.Width,
                    Y1 = groundY,
                    Y2 = groundY,
                    Stroke = Brushes.DarkBlue, // Cor mais visível
                    StrokeThickness = 3 // Linha mais grossa
                };
                
                SimulationCanvas.Children.Add(xAxisLine);
                Canvas.SetZIndex(xAxisLine, 90);
                

                // Adicionar marcações no eixo X (a cada 5 metros)
                for (int i = 5; i <= 100; i += 5)
                {
                    double xPos = i * scale;
                    
                    // Adicionar uma linha de marca
                    Line tick = new Line
                    {
                        X1 = xPos,
                        X2 = xPos,
                        Y1 = groundY - 5,
                        Y2 = groundY + 5,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    
                    // Adicionar texto da marca
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
                    
                    // Adicionar à lista para controle
                    xAxisMarkings.Add(tick);
                    xAxisMarkings.Add(tickLabel);
                }
                
                // Adicionar texto de origem (0)
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
            // Limpar marcações do eixo Y
            foreach (var mark in yAxisMarkings)
            {
                SimulationCanvas.Children.Remove(mark);
            }
            yAxisMarkings.Clear();
            
            if (yAxisLine != null)
            {
                SimulationCanvas.Children.Remove(yAxisLine);
            }
            
            // Limpar marcações do eixo X
            foreach (var mark in xAxisMarkings)
            {
                SimulationCanvas.Children.Remove(mark);
            }
            xAxisMarkings.Clear();
            
            if (xAxisLine != null)
            {
                SimulationCanvas.Children.Remove(xAxisLine);
            }
        }
        
        private void UpdateScaleAndLimits()
        {
            // Ajustar a escala baseada no tamanho da janela
            if (SimulationScroller.ActualWidth > 0 && SimulationScroller.ActualHeight > 0)
            {
                scale = Math.Min(SimulationScroller.ActualWidth / 80, SimulationScroller.ActualHeight / 40);
            }
            
            // Validação para evitar NaN
            if (double.IsNaN(SimulationCanvas.Height))
                return;
                
            // Atualizar a posição do solo
            groundY = SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            // Atualizar todo o sistema de coordenadas
            UpdateCoordinateSystem();
            
            // Se a bola já foi criada, atualizar sua posição
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
            // Atualizar com nova escala e posição do solo
            ClearCoordinateSystem();
            CreateCoordinateSystem();
        }
        
        private void InitializeSimulation()
        {
            // This method isn't needed anymore as we're managing balls individually
            // Instead, we just make sure the existing elements are properly configured
            
            // Clear any existing trajectories and balls from old implementation
            if (trajectoryLine != null)
                SimulationCanvas.Children.Remove(trajectoryLine);
                
            if (ball != null)
                SimulationCanvas.Children.Remove(ball);
                
            trajectory.Clear();
            
            // No need to create new balls here since we're using AddNewBall() method
            
            // Apply global settings
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
            // Parar o timer
            timer.Stop();
            StartButton.Content = "Iniciar";
            time = 0;
            simulationStarted = false;
            
            // Recuperar os parâmetros globais
            if (!TryParseInvariant(GravityTextBox.Text, out g) || g <= 0)
                g = 9.81;
                
            if (!TryParseInvariant(AirResistanceTextBox.Text, out airResistance))
                airResistance = 0.01;
                
            if (!TryParseInvariant(FrictionTextBox.Text, out atritoHorizontal) || 
                atritoHorizontal < 0 || atritoHorizontal > 1)
                atritoHorizontal = 0.95;
            
            // Para cada bola, resetar as trajetórias
            foreach (var ball in balls)
            {
                // Limpar trajetória visual, mas manter os pontos para persistência
                if (ball.Trajectory != null)
                {
                    ball.Trajectory.Points.Clear();
                }
            }
            
            // Aplicar os parâmetros da UI para a bola ativa, se houver alguma
            ApplyUIParametersToBall();
            
            // Centralizar a câmera
            CenterCamera();
        }
        
void ApplyUIParametersToBall()
        {
            if (activeBallIndex < 0 || activeBallIndex >= balls.Count)
                return;

            BallData ball = balls[activeBallIndex];

            // Ler os valores dos controles
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

            // Atualizar o visual da bola
            if (ball.Visual != null)
            {
                ball.Visual.Width = ball.Size;
                ball.Visual.Height = ball.Size;
            }

            // Atualizar posição visual
            UpdateBallPosition(ball);
            UpdateInfoPanel(ball);
            UpdateInfoPanelPosition(ball);
        }
        
        private void CenterCamera()
        {
            if (SimulationScroller == null) return;
            
            try {
                // Garantir que o y-axis está sempre visível
                SimulationScroller.ScrollToHorizontalOffset(0);
                
                // Centralizar verticalmente na bola ou no centro do canvas
                if (ball != null)
                {
                    double ballY = Canvas.GetTop(ball) + ball.Height / 2;
                    cameraOffsetY = Math.Max(0, ballY - SimulationScroller.ViewportHeight / 2);
                    SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
                }
                else
                {
                    // Mostrar o meio do eixo Y
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
                
                // Esconder dica de posicionamento quando iniciar
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
            
            // Get the active ball and set its position
            BallData activeBall = balls[activeBallIndex];
            
            // Converter posição do mouse para coordenadas do mundo
            activeBall.X = position.X / scale;
            activeBall.Y = (groundY - position.Y) / scale;
            
            // Limitar a posição Y para não começar abaixo do solo
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
            
            // Converter posição do mouse para coordenadas do mundo
            activeBall.X = position.X / scale;
            activeBall.Y = (groundY - position.Y) / scale;
            
            // Limitar a posição Y para não começar abaixo do solo
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
            // Verifica se a tecla Ctrl está pressionada para ativar o zoom
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Se Ctrl não estiver pressionado, permite o scroll normal
                return;
            }

            // Check if we have any balls
            if (balls.Count == 0 || activeBallIndex < 0) return;

            e.Handled = true; // Previne o comportamento padrão de scroll

            // Obter a posição do mouse antes do zoom
            Point mousePosition = e.GetPosition(SimulationCanvas);

            // Calcular as coordenadas do mundo antes do zoom (ajustado para x começar em 0)
            double worldX = mousePosition.X / scale;
            double worldY = (groundY - mousePosition.Y) / scale;

            // Alterar a escala com base na direção do scroll
            if (e.Delta > 0)
            {
                // Zoom in
                scale *= 1.1;
            }
            else
            {
                // Zoom out
                scale /= 1.1;
            }

            // Limitar a escala para evitar valores extremos
            scale = Math.Max(1, Math.Min(scale, 50));

            // Atualizar a visualização de todas as bolas
            foreach (var ball in balls)
            {
                UpdateBallPosition(ball);
            }

            // Recalcular a posição do solo para manter consistência com o zoom
            UpdateGroundPosition();

            // Atualizar as trajetórias de todas as bolas
            foreach (var ball in balls)
            {
                UpdateTrajectoryVisual(ball);
            }

            // Atualizar o sistema de coordenadas para a nova escala
            UpdateCoordinateSystem();

            // Atualizar a posição do scrollviewer para manter o ponto sob o cursor
            double newMouseX = worldX * scale;
            double newMouseY = groundY - worldY * scale;

            // Ajustar o scroll para manter a posição relativa ao mouse
            SimulationScroller.ScrollToHorizontalOffset(SimulationScroller.HorizontalOffset + (newMouseX - mousePosition.X));
            SimulationScroller.ScrollToVerticalOffset(SimulationScroller.VerticalOffset + (newMouseY - mousePosition.Y));
        }

        // Add missing method to update trajectory visual for a specific ball
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
                // Log error to debug console
                Console.WriteLine($"Error in UpdateTrajectoryVisual for a ball: {ex.Message}");
            }
        }

        #endregion
        
        private void UpdatePhysics(BallData ball)
        {
            // Atualizar posição e velocidade considerando resistência do ar
            ball.X += ball.Vx * dt;
            ball.Y += ball.Vy * dt - 0.5 * g * dt * dt;
            
            // Não permitir que a bola saia pela esquerda do mundo (eixo Y agora é uma barreira)
            if (ball.X < 0)
            {
                ball.X = 0;
                ball.Vx = -ball.Vx * ball.Restitution; // Quicar na barreira do eixo Y
            }
            
            // Adicionar efeito de resistência do ar
            double vTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            if (vTotal > 0)
            {
                double dragForceMagnitude = airResistance * vTotal * vTotal;
                double dragForceX = -dragForceMagnitude * ball.Vx / vTotal;
                double dragForceY = -dragForceMagnitude * ball.Vy / vTotal;
                
                ball.Vx += dragForceX * dt;
                ball.Vy += dragForceY * dt;
            }
            
            // Efeito da gravidade
            ball.Vy -= g * dt;
            
            // Verificar colisão com o solo (tratamento melhorado para evitar bugs)
            if (ball.Y <= 0.01 && ball.Vy < 0 && ball.Visual != null)
            {
                // Garantir que a bola não fique abaixo do solo
                ball.Y = 0.01;
                
                // Armazenar a velocidade de impacto para efeitos visuais
                double impactVelocity = Math.Abs(ball.Vy);
                
                // Inverter velocidade vertical com perda de energia
                ball.Vy = -ball.Vy * ball.Restitution;
                
                // Reduzir velocidade horizontal devido ao atrito somente em caso de contato com o solo
                ball.Vx *= atritoHorizontal;
                
                // Apenas aplicar o efeito de achatamento se o impacto for significativo
                if (impactVelocity > 2.0)
                {
                    try
                    {
                        // Limitar o achatamento baseado na velocidade de impacto
                        double squashFactor = Math.Min(0.6 + (impactVelocity / 50), 0.8);
                        double stretchFactor = 1.0 + (1.0 - squashFactor);
                        
                        // Efeito de achatamento (mudar escala visual brevemente)
                        ScaleTransform scaleTransform = new ScaleTransform(stretchFactor, squashFactor);
                        ball.Visual.RenderTransform = scaleTransform;
                        
                        // Retorna à forma normal após um breve período
                        DispatcherTimer resetScale = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };
                        
                        resetScale.Tick += (s, e) =>
                        {
                            if (ball.Visual != null)
                            {
                                // Transição suave de volta à forma normal
                                ball.Visual.RenderTransform = null;
                            }
                            resetScale.Stop();
                        };
                        
                        resetScale.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in collision handling: {ex.Message}");
                        // Ensure ball returns to normal state in case of error
                        if (ball.Visual != null)
                            ball.Visual.RenderTransform = null;
                    }
                }
                
                // Se a velocidade for muito baixa, pare completamente para evitar vibrações
                if (Math.Abs(ball.Vy) < 0.2)
                    ball.Vy = 0;
                    
                if (Math.Abs(ball.Vx) < 0.2)
                    ball.Vx = 0;
            }
        }
        
        private void UpdateBallPosition(BallData ball)
        {
            if (ball.Visual == null) return;
            
            // Converter coordenadas do mundo para coordenadas do canvas
            double canvasX = ball.X * scale;
            double canvasY = groundY - ball.Y * scale;
            
            // Atualizar posição da bola (centralizada no ponto)
            Canvas.SetLeft(ball.Visual, canvasX - ball.Visual.Width / 2);
            Canvas.SetTop(ball.Visual, canvasY - ball.Visual.Height / 2);
        }

        private void UpdateCamera(BallData ball)
        {
            if (SimulationScroller == null || ball.Visual == null) return;

            // Obter posição atual da bola
            double ballLeft = Canvas.GetLeft(ball.Visual) + ball.Visual.Width / 2;
            double ballTop = Canvas.GetTop(ball.Visual) + ball.Visual.Height / 2;

            // Calcular a posição ideal para manter o eixo Y no centro e a bola visível
            double idealOffsetX = -SimulationScroller.ViewportWidth / 2;
            double targetOffsetX = Math.Max(0, idealOffsetX);
            
            if (ballLeft > targetOffsetX + SimulationScroller.ViewportWidth * 0.9)
            {
                targetOffsetX = ballLeft - SimulationScroller.ViewportWidth * 0.9;
            }

            // Suavizar movimento da câmera
            cameraOffsetX = cameraOffsetX + (targetOffsetX - cameraOffsetX) * 0.1;
            cameraOffsetY = cameraOffsetY + (ballTop - SimulationScroller.ViewportHeight / 2 - cameraOffsetY) * 0.1;

            // Garantir que a câmera fique dentro dos limites
            cameraOffsetX = Math.Max(0, Math.Min(cameraOffsetX, SimulationCanvas.Width - SimulationScroller.ViewportWidth));
            cameraOffsetY = Math.Max(0, Math.Min(cameraOffsetY, SimulationCanvas.Height - SimulationScroller.ViewportHeight));

            // Atualizar posição do scroll
            if (!double.IsNaN(cameraOffsetX) && !double.IsNaN(cameraOffsetY))
            {
                SimulationScroller.ScrollToHorizontalOffset(cameraOffsetX);
                SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
            }
        }

        private void AddTrajectoryPoint(BallData ball)
        {
            if (ball.Trajectory == null) return;
            
            // Converter coordenadas do mundo para coordenadas do canvas
            double canvasX = ball.X * scale;
            double canvasY = groundY - ball.Y * scale;
            
            // Adicionar ponto à trajetória
            ball.TrajectoryPoints.Add(new Point(canvasX, canvasY));
            
            try {
                // Atualizar a linha de trajetória com todos os pontos (para persistência)
                ball.Trajectory.Points = new PointCollection(ball.TrajectoryPoints);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error updating trajectory points: {ex.Message}");
            }
        }
        
        private void UpdateInfoPanel(BallData ball)
        {
            // Calcular energias
            double velocidadeTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            double energiaCinetica = 0.5 * ball.Mass * velocidadeTotal * velocidadeTotal;
            double energiaPotencial = ball.Mass * g * ball.Y;
            double energiaTotal = energiaCinetica + energiaPotencial;
            
            // Atualizar texto de informação
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
            
            // Posicionar painel de informações próximo à bola
            double ballLeft = Canvas.GetLeft(ball.Visual);
            double ballTop = Canvas.GetTop(ball.Visual);
            
            Canvas.SetLeft(InfoPanel, ballLeft + ball.Visual.Width + 10);
            Canvas.SetTop(InfoPanel, ballTop - InfoPanel.ActualHeight - 5);
        }
        
        private void UpdateTrajectoryVisual()
        {
            // Add null check for trajectoryLine
            if (trajectoryLine == null || trajectory == null || trajectory.Count == 0) return;
            
            try {
                PointCollection newPoints = new PointCollection();
                foreach (Point worldPoint in trajectory)
                {
                    // Recalcular cada ponto da trajetória com a nova escala
                    double canvasX = worldPoint.X;
                    double canvasY = worldPoint.Y;
                    newPoints.Add(new Point(canvasX, canvasY));
                }
                
                trajectoryLine.Points = newPoints;
            }
            catch (Exception ex) {
                // Log error to debug console
                Console.WriteLine($"Error in UpdateTrajectoryVisual: {ex.Message}");
            }
        }
        
        // Adicione este método para manter a posição do solo consistente
        private void UpdateGroundPosition()
        {
            // Garante que a linha do solo sempre fique na posição y=0 do mundo físico
            groundY = SimulationCanvas.Height - 50; // Base offset from bottom
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width; // Ensure ground covers entire width
        }
        
        // Adicione um handler para evento de scroll para garantir que posições sejam atualizadas
        private void SimulationScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Update ball and info panel positions if needed when scroll changes
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count && !isDragging && !timer.IsEnabled)
            {
                BallData activeBall = balls[activeBallIndex];
                UpdateBallPosition(activeBall);
                UpdateInfoPanel(activeBall);
                UpdateInfoPanelPosition(activeBall);
            }
        }
        
        // Evento para o slider de zoom
        private void ZoomSlider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Atualizar a escala com base no valor do slider
            scale = e.NewValue;

            UpdateTrajectoryVisual();
            
        }
        
        // Evento para detectar mudanças nos parâmetros físicos
        private void PhysicsParameter_Changed(object? sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            
            // Não aplicar mudanças se a simulação estiver em andamento
            if (timer.IsEnabled)
            {
                return;
            }
            
            // Validar entrada - verificar se é um número válido
            TextBox? textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                
                // Aceitar formato com vírgula ou ponto para decimal
                double tempValue;
                if (!string.IsNullOrEmpty(text) && !TryParseInvariant(text, out tempValue))
                {
                    // Se não for um número válido, destacar em vermelho
                    textBox.Background = new SolidColorBrush(Colors.LightPink);
                    return;
                }
                else
                {
                    // Se for válido, restaurar cor normal
                    textBox.Background = new SolidColorBrush(Colors.White);
                }
            }
            
            // Aplicar mudanças imediatamente se a simulação não estiver em andamento
            ResetSimulation();
        }

        // Adicionar método para parse de números com cultura invariante (aceita '.' ou ',')
        private bool TryParseInvariant(string text, out double result)
        {
            // Normalize input - accept both period and comma as decimal separators
            string normalizedText = text.Replace(',', '.');
            
            // Use invariant culture for consistent parsing
            return double.TryParse(normalizedText, 
                                  System.Globalization.NumberStyles.Any, 
                                  System.Globalization.CultureInfo.InvariantCulture, 
                                  out result);
        }

        // Método para adicionar uma nova bola
        private void AddNewBall()
        {
            if (balls.Count >= MAX_BALLS)
            {
                MessageBox.Show($"Número máximo de bolas ({MAX_BALLS}) atingido.");
                return;
            }
            
            // Definir as propriedades da nova bola
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
            
            // Criar o elemento visual da bola
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
            
            // Criar a linha de trajetória
            Polyline trajectory = new Polyline
            {
                Stroke = newBall.Color,
                StrokeThickness = 2,
                Opacity = 0.6
            };
            
            SimulationCanvas.Children.Add(trajectory);
            Canvas.SetZIndex(trajectory, 50);
            
            // Atualizar as referências
            newBall.Visual = ballVisual;
            newBall.Trajectory = trajectory;
            
            // Adicionar à lista de bolas
            balls.Add(newBall);
            
            // Tornar esta bola a ativa
            SelectBall(balls.Count - 1);
            
            // Atualizar a posição visual
            UpdateBallPosition(newBall);
            UpdateInfoPanel(newBall);
            UpdateInfoPanelPosition(newBall);
            
            // Atualizar UI
            UpdateBallSelectionUI();
        }
        
        // Método para remover a bola ativa
        private void RemoveActiveBall()
        {
            if (activeBallIndex < 0 || activeBallIndex >= balls.Count)
                return;
                
            // Remover elementos visuais
            if (balls[activeBallIndex].Visual != null)
                SimulationCanvas.Children.Remove(balls[activeBallIndex].Visual);
                
            if (balls[activeBallIndex].Trajectory != null)
                SimulationCanvas.Children.Remove(balls[activeBallIndex].Trajectory);
                
            // Remover da lista
            balls.RemoveAt(activeBallIndex);
            
            // Ajustar índice ativo
            if (balls.Count > 0)
            {
                activeBallIndex = Math.Max(0, activeBallIndex - 1);
                SelectBall(activeBallIndex);
            }
            else
            {
                activeBallIndex = -1;
            }
            
            // Atualizar UI
            UpdateBallSelectionUI();
        }
        
        // Método para selecionar uma bola
        private void SelectBall(int index)
        {
            if (index < 0 || index >= balls.Count)
                return;
                
            activeBallIndex = index;
            
            // Destacar visualmente a bola selecionada e normalizar as outras
            for (int i = 0; i < balls.Count; i++)
            {
                if (balls[i].Visual != null)
                {
                    balls[i].Visual.StrokeThickness = (i == activeBallIndex) ? 3 : 1;
                }
            }
            
            // Atualizar os controles de UI com os valores da bola selecionada
            if (balls[activeBallIndex].Visual != null)
            {
                // Preencher os controles de propriedades
                MassTextBox.Text = balls[activeBallIndex].Mass.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                VxTextBox.Text = balls[activeBallIndex].Vx.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                VyTextBox.Text = balls[activeBallIndex].Vy.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                RestituicaoTextBox.Text = balls[activeBallIndex].Restitution.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                BallSizeTextBox.Text = balls[activeBallIndex].Size.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                
                // Atualizar o painel de informações
                UpdateInfoPanel(balls[activeBallIndex]);
                UpdateInfoPanelPosition(balls[activeBallIndex]);
            }
        }
        
        private void UpdateBallSelectionUI()
        {
            // Este método atualizaria os controles de UI para seleção das bolas
            // Como botões ou combobox que serão adicionados ao XAML
            BallSelector.Items.Clear();
            
            for (int i = 0; i < balls.Count; i++)
            {
                BallSelector.Items.Add($"Bola {i + 1}");
            }
            
            if (activeBallIndex >= 0 && activeBallIndex < balls.Count)
            {
                BallSelector.SelectedIndex = activeBallIndex;
            }
            
            // Ativar/desativar os botões conforme necessário
            RemoveBallButton.IsEnabled = balls.Count > 0;
            AddBallButton.IsEnabled = balls.Count < MAX_BALLS;
        }

        // Event handlers para novos botões
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