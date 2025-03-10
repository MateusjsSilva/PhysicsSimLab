using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
        private readonly double g = 9.81;  // Aceleração da gravidade (m/s²)
        
        // Variáveis de simulação
        private double x, y;       // Posição atual
        private double vx, vy;     // Velocidade atual
        private double time = 0;   // Tempo de simulação
        private readonly double dt = 0.016; // ~60 FPS
        
        // Configurações
        private double coefRestituicao = 0.7;
        private double atritoHorizontal = 0.95;
        
        // Elementos visuais
        private Ellipse ball;
        private readonly List<Point> trajectory = new List<Point>();
        private Polyline trajectoryLine;
        private DispatcherTimer timer;
        
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
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Configura dimensões iniciais
            SetupSimulationCanvas();
            InitializeSimulation();
            
            // Registrar o evento de roda do mouse para zoom
            SimulationCanvas.MouseWheel += SimulationCanvas_MouseWheel;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
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

            // Posiciona o solo no fundo do canvas
            groundY = SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);

            // Configura o canvas para preencher o ScrollViewer
            SimulationCanvas.Width = Math.Max(SimulationScroller.ActualWidth * 2, 2000);
            SimulationCanvas.Height = Math.Max(SimulationScroller.ActualHeight * 2, 1000);
            
            // Log para debug
            Console.WriteLine($"Canvas: Width={SimulationCanvas.Width}, Height={SimulationCanvas.Height}");
            Console.WriteLine($"Ground Y: {groundY}");
        }
        
        private void UpdateScaleAndLimits()
        {
            // Ajustar a escala baseada no tamanho da janela
            if (SimulationScroller.ActualWidth > 0 && SimulationScroller.ActualHeight > 0)
            {
                scale = Math.Min(SimulationScroller.ActualWidth / 80, SimulationScroller.ActualHeight / 40);
            }
            
            // Atualizar a posição do solo
            groundY = SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            
            // Se a bola já foi criada, atualizar sua posição
            if (ball != null)
            {
                UpdateBallPosition();
            }
        }
        
        private void InitializeSimulation()
        {
            // Limpa canvas se necessário (exceto elementos de UI fixos)
            if (trajectoryLine != null)
                SimulationCanvas.Children.Remove(trajectoryLine);
                
            if (ball != null)
                SimulationCanvas.Children.Remove(ball);
                
            trajectory.Clear();
            
            // Adiciona a linha de trajetória
            trajectoryLine = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Opacity = 0.3
            };
            SimulationCanvas.Children.Add(trajectoryLine);
            
            // Adiciona a bola com borda para torná-la mais visível
            ball = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            SimulationCanvas.Children.Add(ball);
            Canvas.SetZIndex(ball, 100);
            
            // Reseta a simulação
            ResetSimulation();
        }
        
        private void ResetSimulation()
        {
            // Posição inicial padrão da bola (será atualizada pelo mouse)
            x = 0;  // metros
            y = 50; // metros
            
            // Pega valores da UI para velocidades
            if (!double.TryParse(VxTextBox.Text, out vx)) vx = 6;
            if (!double.TryParse(VyTextBox.Text, out vy)) vy = 15;
            if (!double.TryParse(RestituicaoTextBox.Text, out coefRestituicao) || 
                coefRestituicao < 0 || coefRestituicao > 1)
                coefRestituicao = 0.7;
            
            // Reseta tempo e estado
            time = 0;
            simulationStarted = false;
            
            // Limpa trajetória
            trajectory.Clear();
            trajectoryLine.Points.Clear();
            
            // Habilita posicionamento da bola
            PositioningHint.Visibility = Visibility.Visible;
            
            // Atualiza a posição visual da bola e infos
            UpdateBallPosition();
            UpdateInfoPanel();
            UpdateInfoPanelPosition();
            
            // Centralizar a visualização no canvas
            CenterCamera();
        }
        
        private void CenterCamera()
        {
            // Centraliza a câmera na posição inicial da bola
            double canvasX = SimulationCanvas.ActualWidth / 2;
            double canvasY = groundY - y * scale;
            
            cameraOffsetX = Math.Max(0, canvasX - SimulationScroller.ViewportWidth / 2);
            cameraOffsetY = Math.Max(0, canvasY - SimulationScroller.ViewportHeight / 2);
            
            SimulationScroller.ScrollToHorizontalOffset(cameraOffsetX);
            SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
        }
        
        #region Eventos UI
        
        private void StartButton_Click(object sender, RoutedEventArgs e)
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
                    PositioningHint.Visibility = Visibility.Collapsed;
                }
            }
        }
        
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            StartButton.Content = "Iniciar";
            ResetSimulation();
        }
        
        private void SimulationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (simulationStarted) return; // Não permitir posicionamento após início
            
            Point position = e.GetPosition(SimulationCanvas);
            isDragging = true;
            
            // Converter posição do mouse para coordenadas do mundo de forma mais precisa
            x = (position.X - SimulationCanvas.ActualWidth / 2) / scale;
            y = (groundY - position.Y) / scale;
            
            // Limitar a posição Y para não começar abaixo do solo
            if (y < 0) y = 0;
            
            UpdateBallPosition();
            UpdateInfoPanel();
        }
        
        private void SimulationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && !simulationStarted)
            {
                Point position = e.GetPosition(SimulationCanvas);
                
                // Converter posição do mouse para coordenadas do mundo
                x = (position.X - SimulationCanvas.ActualWidth / 2) / scale;
                y = (groundY - position.Y) / scale;
                
                // Limitar a posição Y para não começar abaixo do solo
                if (y < 0) y = 0;
                
                UpdateBallPosition();
                UpdateInfoPanel();
                UpdateInfoPanelPosition();
            }
        }
        
        private void SimulationCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }
        
        // Adicione esse evento para detectar o scroll do mouse para zoom
        private void SimulationCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Obter a posição do mouse antes do zoom
            Point mousePosition = e.GetPosition(SimulationCanvas);
            
            // Calcular as coordenadas do mundo antes do zoom
            double worldX = (mousePosition.X - SimulationCanvas.ActualWidth / 2) / scale;
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
            
            // Atualizar a visualização
            UpdateBallPosition();
            
            // Atualizar os elementos visuais da trajetória
            UpdateTrajectoryVisual();
            
            // Atualizar a posição do scrollviewer para manter o ponto sob o cursor
            double newMouseX = worldX * scale + SimulationCanvas.ActualWidth / 2;
            double newMouseY = groundY - worldY * scale;
            
            // Ajustar o scroll para manter a posição relativa ao mouse
            SimulationScroller.ScrollToHorizontalOffset(SimulationScroller.HorizontalOffset + (newMouseX - mousePosition.X));
            SimulationScroller.ScrollToVerticalOffset(SimulationScroller.VerticalOffset + (newMouseY - mousePosition.Y));
            
            e.Handled = true;
        }
        
        #endregion
        
        private void TimerTick(object sender, EventArgs e)
        {
            // Atualiza a física
            UpdatePhysics();
            
            // Atualiza a posição visual da bola
            UpdateBallPosition();
            
            // Adiciona ponto à trajetória
            AddTrajectoryPoint();
            
            // Atualiza o painel de informações
            UpdateInfoPanel();
            UpdateInfoPanelPosition();
            
            // Implementa a camera para seguir a bola
            UpdateCamera();
            
            // Verifica se a simulação deve parar
            if ((Math.Abs(vy) < 0.1 && Math.Abs(vx) < 0.1 && y <= 0.01) || time >= 30)
            {
                timer.Stop();
                StartButton.Content = "Reiniciar";
            }
        }
        
        private void UpdatePhysics()
        {
            // Atualizar posição e velocidade
            x += vx * dt;
            y += vy * dt - 0.5 * g * dt * dt;
            vy -= g * dt;
            
            // Verificar colisão com o solo
            if (y <= 0)
            {
                y = 0;
                vy = -vy * coefRestituicao;  // Inverter velocidade vertical com perda de energia
                vx *= atritoHorizontal;      // Reduzir velocidade horizontal devido ao atrito
                
                // Efeito de achatamento (mudar escala visual brevemente)
                ScaleTransform scaleTransform = new ScaleTransform(1.2, 0.8);
                ball.RenderTransform = scaleTransform;
                
                // Retorna à forma normal após um breve período
                DispatcherTimer resetScale = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                
                resetScale.Tick += (s, e) =>
                {
                    ball.RenderTransform = null;
                    resetScale.Stop();
                };
                
                resetScale.Start();
            }
            
            // Incrementa o tempo
            time += dt;
        }
        
        private void UpdateBallPosition()
        {
            // Converter coordenadas do mundo para coordenadas do canvas
            double canvasX = x * scale + SimulationCanvas.ActualWidth / 2;
            double canvasY = groundY - y * scale;
            
            // Atualizar posição da bola (centralizada no ponto)
            Canvas.SetLeft(ball, canvasX - ball.Width / 2);
            Canvas.SetTop(ball, canvasY - ball.Height / 2);
        }
        
        private void UpdateCamera()
        {
            if (SimulationScroller == null || ball == null) return;
            
            // Obtém a posição atual da bola no canvas
            double ballLeft = Canvas.GetLeft(ball) + ball.Width / 2;
            double ballTop = Canvas.GetTop(ball) + ball.Height / 2;
            
            // Calcula os offsets para manter a bola no centro da viewport
            double targetOffsetX = ballLeft - SimulationScroller.ViewportWidth / 2;
            double targetOffsetY = ballTop - SimulationScroller.ViewportHeight / 2;
            
            // Suaviza o movimento da camera (interpolação)
            cameraOffsetX = cameraOffsetX + (targetOffsetX - cameraOffsetX) * 0.1;
            cameraOffsetY = cameraOffsetY + (targetOffsetY - cameraOffsetY) * 0.1;
            
            // Mantém a camera dentro dos limites do canvas
            cameraOffsetX = Math.Max(0, Math.Min(cameraOffsetX, SimulationCanvas.Width - SimulationScroller.ViewportWidth));
            cameraOffsetY = Math.Max(0, Math.Min(cameraOffsetY, SimulationCanvas.Height - SimulationScroller.ViewportHeight));
            
            // Atualiza a posição da scroll
            SimulationScroller.ScrollToHorizontalOffset(cameraOffsetX);
            SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
        }
        
        private void AddTrajectoryPoint()
        {
            // Converter coordenadas do mundo para coordenadas do canvas
            double canvasX = x * scale + SimulationCanvas.ActualWidth / 2;
            double canvasY = groundY - y * scale;
            
            // Adicionar ponto à trajetória
            trajectory.Add(new Point(canvasX, canvasY));
            
            // Limitar número de pontos para performance
            if (trajectory.Count > 500)
                trajectory.RemoveAt(0);
            
            // Atualizar a linha de trajetória
            trajectoryLine.Points = new PointCollection(trajectory);
        }
        
        private void UpdateInfoPanel()
        {
            // Atualizar texto de informação
            InfoTextBlock.Text = $"Tempo: {time:F2}s\n" +
                                 $"Posição X: {x:F1}m\n" +
                                 $"Altura: {y:F1}m\n" +
                                 $"Velocidade X: {vx:F1}m/s\n" +
                                 $"Velocidade Y: {vy:F1}m/s";
        }
        
        private void UpdateInfoPanelPosition()
        {
            if (ball == null || InfoPanel == null) return;
            
            // Posiciona o painel de informações próximo à bola, mas acima para não cobrir
            double ballLeft = Canvas.GetLeft(ball);
            double ballTop = Canvas.GetTop(ball);
            
            Canvas.SetLeft(InfoPanel, ballLeft + ball.Width + 10);
            Canvas.SetTop(InfoPanel, ballTop - InfoPanel.ActualHeight - 5);
        }
        
        private void UpdateTrajectoryVisual()
        {
            if (trajectory.Count == 0) return;
            
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
    }
}