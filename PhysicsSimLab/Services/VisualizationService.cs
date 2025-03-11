using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PhysicsSimLab.Models;

namespace PhysicsSimLab.Services
{
    public class VisualizationService
    {
        private Canvas simulationCanvas;
        private double groundY;
        private double scale;
        private readonly List<UIElement> yAxisMarkings = new List<UIElement>();
        private readonly List<UIElement> xAxisMarkings = new List<UIElement>();
        private Line? yAxisLine;
        private Line? xAxisLine;
        private Rectangle? groundRectangle;

        public VisualizationService(Canvas canvas)
        {
            simulationCanvas = canvas;
            scale = 10;
            groundY = canvas.Height - 50;
        }

        public double Scale 
        { 
            get => scale; 
            set => scale = value; 
        }

        public double GroundY 
        { 
            get => groundY; 
            set => groundY = value; 
        }

        public void UpdateBallPosition(BallData ball)
        {
            if (ball.Visual == null) return;
            
            double canvasX = ball.X * scale;
            double canvasY = groundY - ball.Y * scale;
            
            Canvas.SetLeft(ball.Visual, canvasX - ball.Visual.Width / 2);
            Canvas.SetTop(ball.Visual, canvasY - ball.Visual.Height / 2);
        }

        public void AddTrajectoryPoint(BallData ball)
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

        public void ClearTrajectory(BallData ball)
        {
            if (ball.Trajectory != null)
            {
                ball.Trajectory.Points.Clear();
                ball.TrajectoryPoints.Clear();
            }
        }

        public void CreateCoordinateSystem()
        {
            ClearCoordinateSystem();
            
            try
            {
                // Criar um retângulo para representar o chão
                if (groundRectangle != null)
                {
                    simulationCanvas.Children.Remove(groundRectangle);
                }
                
                // Garantir que a altura do retângulo do chão seja suficiente para cobrir
                // toda a área abaixo da linha do chão até o final do canvas, com margem extra
                double groundRectHeight = simulationCanvas.Height - groundY + 1000; // Adicionar margem extra
                
                groundRectangle = new Rectangle
                {
                    Width = simulationCanvas.Width,
                    Height = groundRectHeight,
                    Fill = new LinearGradientBrush(
                        Color.FromRgb(139, 69, 19),  // Terra escura no topo
                        Color.FromRgb(210, 180, 140), // Terra mais clara embaixo
                        40),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                
                simulationCanvas.Children.Add(groundRectangle);
                Canvas.SetTop(groundRectangle, groundY);
                Canvas.SetLeft(groundRectangle, 0);
                Canvas.SetZIndex(groundRectangle, 5); // Atrás das bolas mas à frente do fundo
                
                xAxisLine = new Line
                {
                    X1 = 0,
                    X2 = simulationCanvas.Width,
                    Y1 = groundY,
                    Y2 = groundY,
                    Stroke = Brushes.DarkBlue, 
                    StrokeThickness = 3 
                };
                
                simulationCanvas.Children.Add(xAxisLine);
                Canvas.SetZIndex(xAxisLine, 90);
                
                // Create X-axis markings
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
                        FontSize = 12
                    };
                    
                    Canvas.SetLeft(tickLabel, xPos - 8);
                    Canvas.SetTop(tickLabel, groundY + 8);
                    
                    simulationCanvas.Children.Add(tick);
                    simulationCanvas.Children.Add(tickLabel);
                    Canvas.SetZIndex(tick, 50);
                    Canvas.SetZIndex(tickLabel, 50);
                    
                    xAxisMarkings.Add(tick);
                    xAxisMarkings.Add(tickLabel);
                }
                
                TextBlock originLabel = new TextBlock
                {
                    Text = "0"
                };
                Canvas.SetLeft(originLabel, -15);
                Canvas.SetTop(originLabel, groundY + 8);
                simulationCanvas.Children.Add(originLabel);
                Canvas.SetZIndex(originLabel, 50);
                xAxisMarkings.Add(originLabel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating coordinate system: {ex.Message}");
            }
        }

        public void UpdateGroundRect(double width)
        {
            if (groundRectangle != null)
            {
                // Atualiza a largura do retângulo
                groundRectangle.Width = width;
                
                // Também atualiza a altura para garantir que cubra até o fim do canvas
                groundRectangle.Height = simulationCanvas.Height - groundY + 1000; // Adicionar margem extra
                
                // Garante que a posição vertical está correta
                Canvas.SetTop(groundRectangle, groundY);
                Canvas.SetLeft(groundRectangle, 0);
            }
        }

        private void ClearCoordinateSystem()
        {
            foreach (var mark in yAxisMarkings)
            {
                simulationCanvas.Children.Remove(mark);
            }
            yAxisMarkings.Clear();
            
            if (yAxisLine != null)
            {
                simulationCanvas.Children.Remove(yAxisLine);
            }
            
            foreach (var mark in xAxisMarkings)
            {
                simulationCanvas.Children.Remove(mark);
            }
            xAxisMarkings.Clear();
        }
    }
}