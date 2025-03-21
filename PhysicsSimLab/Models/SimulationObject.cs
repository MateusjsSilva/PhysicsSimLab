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

        public SimulationObject(double mass, Vector position, Vector velocity, double radius, string name)
        {
            Mass = mass;
            Position = position;
            Velocity = velocity;
            Acceleration = new Vector(0, 0);
            Radius = radius;
            Name = name;
        }
    }
}
