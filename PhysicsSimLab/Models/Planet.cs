using System.Windows;

namespace PhysicsSimLab.Models
{
    public class Planet : SimulationObject
    {
        public Planet(double mass, Vector position, Vector velocity, double radius, string name) 
            : base(mass, position, velocity, radius, name)
        {
        }
    }
}
