using System.Windows;

namespace PhysicsSimLab.Models
{
    public class SimulationObject
    {
        public double Mass { get; set; }
        public Vector Position { get; set; }
        public Vector Velocity { get; set; }
        public Vector Acceleration { get; set; }
        public double Radius { get; set; }
        public string Name { get; set; } = string.Empty;
        public BallData Ball { get; set; }

        public SimulationObject(double mass, Vector position, Vector velocity, double radius, string name)
        {
            Mass = mass;
            Position = position;
            Velocity = velocity;
            Acceleration = new Vector(0, 0);
            Radius = radius;
            Name = name;
            
            // Initialize BallData
            Ball = new BallData
            {
                X = position.X,
                Y = position.Y,
                Vx = velocity.X,
                Vy = velocity.Y,
                Mass = mass,
                Size = radius * 2,
                InitialX = position.X,
                InitialY = position.Y,
                InitialVx = velocity.X,
                InitialVy = velocity.Y
            };
        }
        
        public void UpdateFromBall()
        {
            Position = new Vector(Ball.X, Ball.Y);
            Velocity = new Vector(Ball.Vx, Ball.Vy);
        }
        
        public void UpdateBall()
        {
            Ball.X = Position.X;
            Ball.Y = Position.Y;
            Ball.Vx = Velocity.X;
            Ball.Vy = Velocity.Y;
            
            if (Ball.TrajectoryPoints.Count < 1000)
            {
                Ball.TrajectoryPoints.Add(new Point(Ball.X, Ball.Y));
            }
        }
    }
}