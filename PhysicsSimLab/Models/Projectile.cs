using System.Windows;

namespace PhysicsSimLab.Models
{
    public class Projectile : SimulationObject
    {
        public Projectile(double mass, Vector position, Vector velocity, double radius) 
            : base(mass, position, velocity, radius, "Projectile")
        {
        }
    }
}
