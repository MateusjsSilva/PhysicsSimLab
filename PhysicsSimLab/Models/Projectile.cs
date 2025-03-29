using System.Windows;
using System.Windows.Media;

namespace PhysicsSimLab.Models
{
    public class Projectile : SimulationObject
    {
        public Projectile(double mass, Vector position, Vector velocity, double radius, string name = "Projectile") 
            : base(mass, position, velocity, radius, name)
        {
            Ball.Color = new SolidColorBrush(Colors.Red);
        }
    }
}