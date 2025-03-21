using System;
using System.Collections.Generic;
using System.Windows;
using PhysicsSimLab.Models;

namespace PhysicsSimLab.Core
{
    public enum SimulationType
    {
        ProjectileMotion,
        PlanetaryOrbit
    }

    public class PhysicsEngine
    {
        public const double G = 6.67430e-11;
        public const double EarthG = 9.81;

        private SimulationType _currentSimulation;
        private List<SimulationObject> _objects = new();
        private double _timeStep = 0.01;

        public SimulationType CurrentSimulation
        {
            get => _currentSimulation;
            set
            {
                _currentSimulation = value;
                ResetSimulation();
            }
        }

        public List<SimulationObject> Objects => _objects;
        
        public double TimeStep
        {
            get => _timeStep;
            set => _timeStep = value;
        }

        public void AddObject(SimulationObject obj)
        {
            _objects.Add(obj);
        }

        public void ClearObjects()
        {
            _objects.Clear();
        }

        public void StepSimulation()
        {
            switch (_currentSimulation)
            {
                case SimulationType.ProjectileMotion:
                    StepProjectileSimulation();
                    break;
                case SimulationType.PlanetaryOrbit:
                    StepPlanetarySimulation();
                    break;
            }
        }

        private void StepProjectileSimulation()
        {
            foreach (var obj in _objects)
            {
                obj.Acceleration = new Vector(0, -EarthG);
                
                obj.Velocity += obj.Acceleration * _timeStep;
                
                obj.Position += obj.Velocity * _timeStep;

                if (obj.Position.Y < 0)
                {
                    obj.Position = new Vector(obj.Position.X, 0);
                    obj.Velocity = new Vector(obj.Velocity.X * 0.8, -obj.Velocity.Y * 0.8);
                }
            }
        }

        private void StepPlanetarySimulation()
        {
            foreach (var obj in _objects)
            {
                obj.Acceleration = new Vector(0, 0);

                foreach (var other in _objects)
                {
                    if (obj == other) continue;

                    var direction = other.Position - obj.Position;
                    var distance = direction.Length;
                    
                    if (distance < 0.1) continue;
                    
                    var forceMagnitude = G * obj.Mass * other.Mass / (distance * distance);
                    var forceDirection = direction / distance;
                    var force = forceDirection * forceMagnitude;
                    
                    obj.Acceleration += force / obj.Mass;
                }

                obj.Velocity += obj.Acceleration * _timeStep;
                
                obj.Position += obj.Velocity * _timeStep;
            }
        }

        public void ResetSimulation()
        {
            ClearObjects();
        }
    }
}
