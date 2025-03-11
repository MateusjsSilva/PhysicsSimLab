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
                yAxisLine = new Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = groundY,
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 3 
                };
                
                simulationCanvas.Children.Add(yAxisLine);
                Canvas.SetZIndex(yAxisLine, 90); 
                Canvas.SetLeft(yAxisLine, 0); 

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
                        FontSize = 10
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
                    Text = "0",
                    FontSize = 10
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
