using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhysicsSimLab.Models;
using PhysicsSimLab.Helpers;

namespace PhysicsSimLab.Services
{
    public class SimulationService
    {
        private readonly PhysicsService physicsService;
        private readonly VisualizationService visualizationService;
        private DispatcherTimer timer;
        private List<BallData> balls;
        private bool simulationStarted = false;
        private double time = 0;
        
        public event Action<double> TimeUpdated;
        public event Action SimulationStopped;
        
        public SimulationService(PhysicsService physicsService, VisualizationService visualizationService, List<BallData> balls)
        {
            this.physicsService = physicsService;
            this.visualizationService = visualizationService;
            this.balls = balls;
            
            timer = new DispatcherTimer();
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(physicsService.Dt);
        }

        public bool IsRunning => timer.IsEnabled;
        public double CurrentTime => time;
        
        public void Start()
        {
            timer.Start();
            if (!simulationStarted)
            {
                simulationStarted = true;
            }
        }
        
        public void Stop()
        {
            timer.Stop();
        }
        
        public void Reset()
        {
            timer.Stop();
            time = 0;
            simulationStarted = false;
            
            // Reset balls to initial positions
            foreach (var ball in balls)
            {
                visualizationService.ClearTrajectory(ball);
                
                ball.X = ball.InitialX;
                ball.Y = ball.InitialY;
                ball.Vx = ball.InitialVx;
                ball.Vy = ball.InitialVy;
                
                visualizationService.UpdateBallPosition(ball);
            }
            
            TimeUpdated?.Invoke(time);
        }
        
        private void TimerTick(object? sender, EventArgs e)
        {
            if (balls.Count == 0)
            {
                timer.Stop();
                return;
            }
            
            bool allStopped = true;
            
            foreach (var ball in balls)
            {
                if (ball.Visual == null || ball.Trajectory == null) 
                    continue;
                
                physicsService.UpdatePhysics(ball);
                
                physicsService.HandleGroundCollision(ball, 0.01, transform => {
                    if (ball.Visual != null)
                    {
                        ball.Visual.RenderTransform = transform;
                        
                        DispatcherTimer resetScale = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };
                        
                        resetScale.Tick += (s, e) =>
                        {
                            if (ball.Visual != null)
                                ball.Visual.RenderTransform = null;
                            
                            resetScale.Stop();
                        };
                        
                        resetScale.Start();
                    }
                });
                
                visualizationService.UpdateBallPosition(ball);
                visualizationService.AddTrajectoryPoint(ball);
                
                if (physicsService.IsBallMoving(ball))
                {
                    allStopped = false;
                }
            }
            
            time += physicsService.Dt;
            TimeUpdated?.Invoke(time);
            
            if (allStopped || time >= 30)
            {
                timer.Stop();
                SimulationStopped?.Invoke();
            }
        }
    }
}
