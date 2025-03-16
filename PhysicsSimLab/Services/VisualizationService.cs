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
                if (groundRectangle != null)
                {
                    simulationCanvas.Children.Remove(groundRectangle);
                }
                
                double groundRectHeight = simulationCanvas.Height - groundY + 1000;

                // Create a vertical linear gradient that's lighter at top and darker at bottom
                LinearGradientBrush groundGradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                
                // Add gradient stops - lighter color at top (0.0), darker at bottom (1.0)
                groundGradient.GradientStops.Add(new GradientStop(Color.FromRgb(210, 180, 140), 0.0)); // Light sandy/tan color
                groundGradient.GradientStops.Add(new GradientStop(Color.FromRgb(139, 69, 19), 1.0));  // Dark brown
                
                groundRectangle = new Rectangle
                {
                    Width = simulationCanvas.Width,
                    Height = groundRectHeight,
                    Fill = groundGradient,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                
                simulationCanvas.Children.Add(groundRectangle);
                Canvas.SetTop(groundRectangle, groundY);
                Canvas.SetLeft(groundRectangle, 0);
                Canvas.SetZIndex(groundRectangle, 5);
                
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
                
                yAxisLine = new Line
                {
                    X1 = 0,
                    X2 = 0,
                    Y1 = groundY,
                    Y2 = 0, 
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 3
                };
                
                simulationCanvas.Children.Add(yAxisLine);
                Canvas.SetZIndex(yAxisLine, 90);
                
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
                
                for (int i = 5; i <= 50; i += 5)
                {
                    double yPos = groundY - i * scale;
                    
                    if (yPos < 0) break;
                    
                    Line tick = new Line
                    {
                        X1 = -5,
                        X2 = 5,
                        Y1 = yPos,
                        Y2 = yPos,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    
                    TextBlock tickLabel = new TextBlock
                    {
                        Text = i.ToString(),
                        FontSize = 12,
                        Foreground = Brushes.Black,
                        FontWeight = FontWeights.SemiBold,
                        Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                    };
                    
                    Canvas.SetLeft(tickLabel, -20);
                    Canvas.SetTop(tickLabel, yPos - 8);
                    
                    simulationCanvas.Children.Add(tick);
                    simulationCanvas.Children.Add(tickLabel);
                    Canvas.SetZIndex(tick, 50);
                    Canvas.SetZIndex(tickLabel, 95);
                    
                    yAxisMarkings.Add(tick);
                    yAxisMarkings.Add(tickLabel);
                }
                
                TextBlock originLabel = new TextBlock
                {
                    Text = "0",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = Brushes.DarkBlue,
                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                };
                Canvas.SetLeft(originLabel, -20);
                Canvas.SetTop(originLabel, groundY + 8);
                simulationCanvas.Children.Add(originLabel);
                Canvas.SetZIndex(originLabel, 95);
                xAxisMarkings.Add(originLabel);
                
                TextBlock xAxisLabel = new TextBlock
                {
                    Text = "X (m)",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = Brushes.DarkBlue
                };
                Canvas.SetLeft(xAxisLabel, simulationCanvas.Width - 50);
                Canvas.SetTop(xAxisLabel, groundY + 15);
                simulationCanvas.Children.Add(xAxisLabel);
                Canvas.SetZIndex(xAxisLabel, 95);
                xAxisMarkings.Add(xAxisLabel);
                
                TextBlock yAxisLabel = new TextBlock
                {
                    Text = "Y (m)",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = Brushes.DarkBlue
                };
                Canvas.SetLeft(yAxisLabel, 10);
                Canvas.SetTop(yAxisLabel, 10);
                simulationCanvas.Children.Add(yAxisLabel);
                Canvas.SetZIndex(yAxisLabel, 95);
                yAxisMarkings.Add(yAxisLabel);
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
                groundRectangle.Width = width;
                
                groundRectangle.Height = simulationCanvas.Height - groundY + 1000;
                
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