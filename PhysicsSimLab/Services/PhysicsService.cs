using System;
using System.Windows.Media;
using PhysicsSimLab.Models;

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
                double sizeFactor = ball.Size / 30.0;
                double dragForceMagnitude = AirResistance * vTotal * vTotal * sizeFactor * sizeFactor;
                double dragAccelerationFactor = dragForceMagnitude / ball.Mass;
                double dragForceX = -dragAccelerationFactor * ball.Vx / vTotal;
                double dragForceY = -dragAccelerationFactor * ball.Vy / vTotal;
                
                ball.Vx += dragForceX * Dt;
                ball.Vy += dragForceY * Dt;
            }
            
            ball.Vy -= Gravity * Dt;
        }

        public void HandleGroundCollision(BallData ball, double groundLevel, Action<ScaleTransform> applyTransform)
        {
            double effectiveGroundLevel = groundLevel + (ball.Size / 100.0);
            
            if (ball.Y <= effectiveGroundLevel && ball.Vy < 0 && ball.Visual != null)
            {
                ball.Y = effectiveGroundLevel;
                
                double impactVelocity = Math.Abs(ball.Vy);
                double effectiveRestitution = ball.Restitution * (1.0 - 0.05 * Math.Min(ball.Mass - 1.0, 0.5));
                
                ball.Vy = -ball.Vy * effectiveRestitution;
                
                double effectiveFriction = FrictionCoefficient * (1.0 - 0.02 * Math.Min(ball.Mass - 1.0, 1.0));
                ball.Vx *= effectiveFriction;
                
                if (impactVelocity > 2.0)
                {
                    try
                    {
                        double massFactor = Math.Max(0.7, Math.Min(1.0 / ball.Mass, 1.2));
                        double squashFactor = Math.Min(0.6 + (impactVelocity / 50), 0.8) * massFactor;
                        double stretchFactor = 1.0 + (1.0 - squashFactor);
                        
                        ScaleTransform scaleTransform = new ScaleTransform(stretchFactor, squashFactor);
                        applyTransform(scaleTransform);
                    }
                    catch 
                    {

                    }
                }
                
                double stopThreshold = 0.3 - (0.05 * Math.Min(ball.Mass - 1.0, 0.25));
                
                if (Math.Abs(ball.Vy) < stopThreshold)
                    ball.Vy = 0;
                    
                if (Math.Abs(ball.Vx) < stopThreshold)
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