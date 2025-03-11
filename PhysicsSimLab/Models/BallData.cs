using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PhysicsSimLab.Models
{
    public class BallData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Mass { get; set; }
        public double Restitution { get; set; }
        public double Size { get; set; }
        public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.Black);
        public Ellipse? Visual { get; set; }
        public Polyline? Trajectory { get; set; }
        public List<Point> TrajectoryPoints { get; set; } = [];
        public double InitialX { get; set; }
        public double InitialY { get; set; }
        public double InitialVx { get; set; }
        public double InitialVy { get; set; }
        
        public BallData Clone()
        {
            return new BallData
            {
                X = X,
                Y = Y,
                Vx = Vx,
                Vy = Vy,
                Mass = Mass,
                Restitution = Restitution,
                Size = Size,
                Color = new SolidColorBrush(Color.Color),
                InitialX = InitialX,
                InitialY = InitialY,
                InitialVx = InitialVx,
                InitialVy = InitialVy
            };
        }
    }
}