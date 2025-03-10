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
        
        // Variáveis de simulação
        private double x, y;       // Posição atual
        private double vx, vy;     // Velocidade atual
        private double time = 0;   // Tempo de simulação
        private readonly double dt = 0.016; // ~60 FPS
        
        // Configurações
        private double massa = 1.0;     // Massa da bola (kg)
        private double coefRestituicao = 0.7;
        private double atritoHorizontal = 0.95;
        private double airResistance = 0.01;
        private double ballSize = 30;
        
        // Elementos visuais
        private Ellipse? ball;  // Mark as nullable
        private readonly List<Point> trajectory = new List<Point>();
        private Polyline? trajectoryLine;  // Mark as nullable
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
            
            // Começar em tela cheia
            WindowState = WindowState.Maximized;
        }

        // Update method signature to match EventHandler delegate
        private void TimerTick(object? sender, EventArgs e)
        {
            // Safety check for ball and trajectoryLine
            if (ball == null || trajectoryLine == null) 
            {
                timer.Stop();
                return;
            }
            
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

        // Make sure all methods that can receive null parameters have proper signatures
        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Configura dimensões iniciais
            SetupSimulationCanvas();
            InitializeSimulation();  // Make sure this runs completely
            
            // Registrar o evento de roda do mouse para zoom
            SimulationCanvas.MouseWheel += SimulationCanvas_MouseWheel;
            
            // Apply the thin scrollbar style
            Style scrollBarStyle = (Style)FindResource("ThinScrollBar");
            SimulationScroller.Resources[typeof(ScrollBar)] = scrollBarStyle;
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
            
            // Position ground at the bottom
            groundY = SimulationCanvas.Height - 50;
            Canvas.SetTop(GroundLine, groundY);
            GroundLine.Width = SimulationCanvas.Width;
            
            // Ajusta o scroll para mostrar a área positiva do canvas
            SimulationScroller.ScrollToHorizontalOffset(0);
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
            GroundLine.Width = SimulationCanvas.Width;
            
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
            
            // Recuperar valores adicionais dos parâmetros com cultura invariante
            if (!TryParseInvariant(MassTextBox.Text, out massa) || massa <= 0)
                massa = 1.0;
                
            if (!TryParseInvariant(GravityTextBox.Text, out g) || g <= 0)
                g = 9.81;
                
            if (!TryParseInvariant(AirResistanceTextBox.Text, out airResistance))
                airResistance = 0.01;
                
            if (!TryParseInvariant(FrictionTextBox.Text, out atritoHorizontal) || 
                atritoHorizontal < 0 || atritoHorizontal > 1)
                atritoHorizontal = 0.95;
                
            if (!TryParseInvariant(BallSizeTextBox.Text, out ballSize) || ballSize <= 0)
                ballSize = 30;
                
            // Ensure ball has the correct size immediately
            if (ball != null)
            {
                ball.Width = ballSize;
                ball.Height = ballSize;
            }
        }
        
        private void ResetSimulation()
        {
            // Posição inicial padrão da bola (será atualizada pelo mouse)
            x = 0;  // metros
            y = 10; // metros
            
            // Pega valores da UI para velocidades e outros parâmetros usando cultura invariante
            if (!TryParseInvariant(MassTextBox.Text, out massa) || massa <= 0)
                massa = 1.0;
                
            if (!TryParseInvariant(VxTextBox.Text, out vx)) vx = 6;
            if (!TryParseInvariant(VyTextBox.Text, out vy)) vy = 15;
            if (!TryParseInvariant(RestituicaoTextBox.Text, out coefRestituicao) || 
                coefRestituicao < 0 || coefRestituicao > 1)
                coefRestituicao = 0.7;
            
            // Recuperar valores adicionais dos parâmetros com cultura invariante
            if (!TryParseInvariant(GravityTextBox.Text, out g) || g <= 0)
                g = 9.81;
                
            if (!TryParseInvariant(AirResistanceTextBox.Text, out airResistance))
                airResistance = 0.01;
                
            if (!TryParseInvariant(FrictionTextBox.Text, out atritoHorizontal) || 
                atritoHorizontal < 0 || atritoHorizontal > 1)
                atritoHorizontal = 0.95;
                
            if (!TryParseInvariant(BallSizeTextBox.Text, out ballSize) || ballSize <= 0)
                ballSize = 30;
                
            // Atualizar tamanho da bola
            if (ball != null)
            {
                ball.Width = ballSize;
                ball.Height = ballSize;
            }
            
            // Reseta tempo e estado
            time = 0;
            simulationStarted = false;
            
            // Limpa trajetória
            trajectory.Clear();
            if (trajectoryLine != null) // Fix for line 257 - add null check
            {
                trajectoryLine.Points.Clear();
            }
            
            // Atualiza a posição visual da bola e infos
            UpdateBallPosition();
            UpdateInfoPanel();
            UpdateInfoPanelPosition();
            
            // Centralizar a visualização no canvas
            CenterCamera();
        }
        
        private void CenterCamera()
        {
            if (SimulationScroller == null) return;
            
            // Centraliza a câmera na posição inicial da bola
            double canvasX = x * scale;
            double canvasY = groundY - y * scale;
            
            try {
                // Ajusta a visualização para manter a bola no centro
                cameraOffsetX = Math.Max(0, canvasX - SimulationScroller.ViewportWidth / 2);
                cameraOffsetY = Math.Max(0, canvasY - SimulationScroller.ViewportHeight / 2);
                
                SimulationScroller.ScrollToHorizontalOffset(cameraOffsetX);
                SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
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
            if (simulationStarted || ball == null) return; // Add ball null check
            
            Point position = e.GetPosition(SimulationCanvas);
            isDragging = true;
            
            // Converter posição do mouse para coordenadas do mundo (x começa em 0 na esquerda)
            x = position.X / scale;
            y = (groundY - position.Y) / scale;
            
            // Limitar a posição Y para não começar abaixo do solo
            if (y < 0) y = 0;
            
            UpdateBallPosition();
            UpdateInfoPanel();
        }
        
        private void SimulationCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isDragging || simulationStarted || ball == null) return; // Add ball null check
            
            Point position = e.GetPosition(SimulationCanvas);
            
            // Converter posição do mouse para coordenadas do mundo (x começa em 0 na esquerda)
            x = position.X / scale;
            y = (groundY - position.Y) / scale;
            
            // Limitar a posição Y para não começar abaixo do solo
            if (y < 0) y = 0;
            
            UpdateBallPosition();
            UpdateInfoPanel();
            UpdateInfoPanelPosition();
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

            if (ball == null) return;

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

            // Atualizar a visualização
            UpdateBallPosition();

            // Recalcular a posição do solo para manter consistência com o zoom
            UpdateGroundPosition();

            // Atualizar os elementos visuais da trajetória
            UpdateTrajectoryVisual();

            // Atualizar a posição do scrollviewer para manter o ponto sob o cursor
            double newMouseX = worldX * scale;
            double newMouseY = groundY - worldY * scale;

            // Ajustar o scroll para manter a posição relativa ao mouse
            SimulationScroller.ScrollToHorizontalOffset(SimulationScroller.HorizontalOffset + (newMouseX - mousePosition.X));
            SimulationScroller.ScrollToVerticalOffset(SimulationScroller.VerticalOffset + (newMouseY - mousePosition.Y));
        }

        #endregion
        
        private void UpdatePhysics()
        {
            // Atualizar posição e velocidade considerando resistência do ar
            x += vx * dt;
            y += vy * dt - 0.5 * g * dt * dt;
            
            // Não permitir que a bola saia pela esquerda do mundo
            if (x < 0)
            {
                x = 0;
                vx = -vx * coefRestituicao; // Bounce da parede esquerda
            }
            
            // Adicionar efeito de resistência do ar
            double vTotal = Math.Sqrt(vx * vx + vy * vy);
            if (vTotal > 0)
            {
                double dragForceMagnitude = airResistance * vTotal * vTotal;
                double dragForceX = -dragForceMagnitude * vx / vTotal;
                double dragForceY = -dragForceMagnitude * vy / vTotal;
                
                vx += dragForceX * dt;
                vy += dragForceY * dt;
            }
            
            // Efeito da gravidade
            vy -= g * dt;
            
            // Verificar colisão com o solo (tratamento melhorado para evitar bugs)
            if (y <= 0.01 && vy < 0 && ball != null)
            {
                // Garantir que a bola não fique abaixo do solo
                y = 0.01;
                
                // Armazenar a velocidade de impacto para efeitos visuais
                double impactVelocity = Math.Abs(vy);
                
                // Inverter velocidade vertical com perda de energia
                vy = -vy * coefRestituicao;
                
                // Reduzir velocidade horizontal devido ao atrito somente em caso de contato com o solo
                vx *= atritoHorizontal;
                
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
                        ball.RenderTransform = scaleTransform;
                        
                        // Retorna à forma normal após um breve período
                        DispatcherTimer resetScale = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };
                        
                        resetScale.Tick += (s, e) =>
                        {
                            if (ball != null)
                            {
                                // Transição suave de volta à forma normal
                                ball.RenderTransform = null;
                            }
                            resetScale.Stop();
                        };
                        
                        resetScale.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in collision handling: {ex.Message}");
                        // Ensure ball returns to normal state in case of error
                        if (ball != null)
                            ball.RenderTransform = null;
                    }
                }
                
                // Se a velocidade for muito baixa, pare completamente para evitar vibrações
                if (Math.Abs(vy) < 0.2)
                    vy = 0;
                    
                if (Math.Abs(vx) < 0.2)
                    vx = 0;
            }
            
            // Incrementa o tempo
            time += dt;
        }
        
        private void UpdateBallPosition()
        {
            // Add a null check to prevent NullReferenceException
            if (ball == null) return;
            
            // Converter coordenadas do mundo para coordenadas do canvas (x começa em 0 na esquerda)
            double canvasX = x * scale;
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

            // Verifica se os offsets são válidos antes de atualizar a posição da scroll
            if (!double.IsNaN(cameraOffsetX) && !double.IsNaN(cameraOffsetY))
            {
                SimulationScroller.ScrollToHorizontalOffset(cameraOffsetX);
                SimulationScroller.ScrollToVerticalOffset(cameraOffsetY);
            }
        }


        private void AddTrajectoryPoint()
        {
            // Add null check for trajectoryLine
            if (trajectoryLine == null) return;
            
            // Converter coordenadas do mundo para coordenadas do canvas (x começa em 0 na esquerda)
            double canvasX = x * scale;
            double canvasY = groundY - y * scale;
            
            // Adicionar ponto à trajetória
            trajectory.Add(new Point(canvasX, canvasY));
            
            // Limitar número de pontos para performance
            if (trajectory.Count > 500)
                trajectory.RemoveAt(0);
            
            try {
                // Atualizar a linha de trajetória - wrap in try/catch to prevent crashes
                trajectoryLine.Points = new PointCollection(trajectory);
            }
            catch (Exception ex) {
                // Log error to debug console
                Console.WriteLine($"Error updating trajectory points: {ex.Message}");
            }
        }
        
        private void UpdateInfoPanel()
        {
            // Calcular energias
            double velocidadeTotal = Math.Sqrt(vx * vx + vy * vy);
            double energiaCinetica = 0.5 * massa * velocidadeTotal * velocidadeTotal;
            double energiaPotencial = massa * g * y;
            double energiaTotal = energiaCinetica + energiaPotencial;
            
            // Atualizar texto de informação com fonte maior
            InfoTextBlock.FontSize = 14; // Aumentar tamanho da fonte
            InfoTextBlock.FontWeight = FontWeights.SemiBold; // Opcionalmente tornar fonte mais nítida
            InfoTextBlock.Text = $"Tempo: {time:F2}s\n" +
                                 $"Posição X: {x:F1}m\n" +
                                 $"Altura: {y:F1}m\n" +
                                 $"Velocidade X: {vx:F1}m/s\n" +
                                 $"Velocidade Y: {vy:F1}m/s\n" +
                                 $"Massa: {massa:F1}kg\n" +
                                 $"E. Cinética: {energiaCinetica:F1}J\n" +
                                 $"E. Potencial: {energiaPotencial:F1}J\n" +
                                 $"E. Total: {energiaTotal:F1}J";
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
            if (ball != null && !isDragging && !timer.IsEnabled)
            {
                UpdateBallPosition();
                UpdateInfoPanelPosition();
            }
        }
        
        // Evento para o slider de zoom
        private void ZoomSlider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Atualizar a escala com base no valor do slider
            scale = e.NewValue;
            
            // Atualizar a visualização
            UpdateBallPosition();
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
                if (!string.IsNullOrEmpty(text) && !TryParseInvariant(text, out _))
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
    }
}