using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PhysicsSimLab.Models;
using PhysicsSimLab.Helpers;

namespace PhysicsSimLab.Services
{
    public class PhysicsService
    {
        public double Gravity { get; set; } = 9.81;
        public double AirResistance { get; set; } = 0.01;
        public double FrictionCoefficient { get; set; } = 0.95;
        public double Dt { get; } = 0.016;

        public void UpdatePhysics(BallData ball)
        {
            ball.X += ball.Vx * Dt;
            ball.Y += ball.Vy * Dt - 0.5 * Gravity * Dt * Dt;
            
            if (ball.X < 0)
            {
                ball.X = 0;
                ball.Vx = -ball.Vx * ball.Restitution;
            }
            
            double vTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            if (vTotal > 0)
            {
                double dragForceMagnitude = AirResistance * vTotal * vTotal;
                double dragForceX = -dragForceMagnitude * ball.Vx / vTotal;
                double dragForceY = -dragForceMagnitude * ball.Vy / vTotal;
                
                ball.Vx += dragForceX * Dt;
                ball.Vy += dragForceY * Dt;
            }
            
            ball.Vy -= Gravity * Dt;
        }

        public void HandleGroundCollision(BallData ball, double groundLevel, Action<ScaleTransform> applyTransform)
        {
            if (ball.Y <= groundLevel && ball.Vy < 0 && ball.Visual != null)
            {
                ball.Y = groundLevel;
                
                double impactVelocity = Math.Abs(ball.Vy);
                
                ball.Vy = -ball.Vy * ball.Restitution;
                ball.Vx *= FrictionCoefficient;
                
                if (impactVelocity > 2.0)
                {
                    double squashFactor = Math.Min(0.6 + (impactVelocity / 50), 0.8);
                    double stretchFactor = 1.0 + (1.0 - squashFactor);
                    
                    ScaleTransform scaleTransform = new ScaleTransform(stretchFactor, squashFactor);
                    applyTransform(scaleTransform);
                }
                
                if (Math.Abs(ball.Vy) < 0.3)
                    ball.Vy = 0;
                    
                if (Math.Abs(ball.Vx) < 0.3)
                    ball.Vx = 0;
            }
        }

        public bool IsBallMoving(BallData ball, double threshold = 0.1)
        {
            return Math.Abs(ball.Vy) >= threshold || Math.Abs(ball.Vx) >= threshold || ball.Y > threshold;
        }

        public double CalculateKineticEnergy(BallData ball)
        {
            double velocidadeTotal = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            return 0.5 * ball.Mass * velocidadeTotal * velocidadeTotal;
        }

        public double CalculatePotentialEnergy(BallData ball)
        {
            return ball.Mass * Gravity * ball.Y;
        }
    }
}
