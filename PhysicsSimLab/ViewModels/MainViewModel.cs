using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using PhysicsSimLab.Core;
using PhysicsSimLab.Models;

namespace PhysicsSimLab.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PhysicsEngine _engine = new();
        private readonly DispatcherTimer _timer = new();
        private bool _isSimulationRunning;
        private SimulationType _selectedSimulationType;
        private bool _isSidebarExpanded = true;
        private int _maxBalls = 5;
        private List<Color> _ballColors = new List<Color> 
        { 
            Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Purple 
        };
        private SimulationObjectViewModel? _selectedBall;
        private double _gravity = 9.81;
        private double _airResistance = 0.01;
        private double _friction = 0.95;
        
        // Projectile properties
        private double _initialHeight = 10;
        private double _initialVelocity = 15;
        private double _launchAngle = 45;
        
        // Planetary properties
        private double _planetMass = 5.97e24;
        private double _satelliteMass = 7.34e22;
        private double _orbitRadius = 3.84e8;
        private double _orbitVelocity = 1022;

        public MainViewModel()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += Timer_Tick;
            
            StartCommand = new RelayCommand(_ => StartSimulation());
            StopCommand = new RelayCommand(_ => StopSimulation());
            ResetCommand = new RelayCommand(_ => ResetSimulation());
            ToggleSidebarCommand = new RelayCommand(_ => IsSidebarExpanded = !IsSidebarExpanded);
            AddBallCommand = new RelayCommand(_ => AddBall(), _ => CanAddBall());
            RemoveBallCommand = new RelayCommand(_ => RemoveBall(), _ => CanRemoveBall());
            
            SimulationObjects = new ObservableCollection<SimulationObjectViewModel>();
            
            SelectedSimulationType = SimulationType.ProjectileMotion;
            ResetSimulation();
            
            // Set engine parameters
            _engine.EarthG = _gravity;
            _engine.AirResistance = _airResistance;
            _engine.Friction = _friction;
        }

        public ObservableCollection<SimulationObjectViewModel> SimulationObjects { get; }

        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set
            {
                if (_isSidebarExpanded != value)
                {
                    _isSidebarExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public SimulationType SelectedSimulationType
        {
            get => _selectedSimulationType;
            set
            {
                if (_selectedSimulationType != value)
                {
                    _selectedSimulationType = value;
                    _engine.CurrentSimulation = value;
                    OnPropertyChanged();
                    ResetSimulation();
                }
            }
        }

        public bool IsSimulationRunning
        {
            get => _isSimulationRunning;
            set
            {
                _isSimulationRunning = value;
                OnPropertyChanged();
            }
        }

        public double InitialHeight
        {
            get => _initialHeight;
            set
            {
                _initialHeight = value;
                OnPropertyChanged();
            }
        }

        public double InitialVelocity
        {
            get => _initialVelocity;
            set
            {
                _initialVelocity = value;
                OnPropertyChanged();
            }
        }

        public double LaunchAngle
        {
            get => _launchAngle;
            set
            {
                _launchAngle = value;
                OnPropertyChanged();
            }
        }

        public double PlanetMass
        {
            get => _planetMass;
            set
            {
                _planetMass = value;
                OnPropertyChanged();
            }
        }

        public double SatelliteMass
        {
            get => _satelliteMass;
            set
            {
                _satelliteMass = value;
                OnPropertyChanged();
            }
        }

        public double OrbitRadius
        {
            get => _orbitRadius;
            set
            {
                _orbitRadius = value;
                OnPropertyChanged();
            }
        }

        public double OrbitVelocity
        {
            get => _orbitVelocity;
            set
            {
                _orbitVelocity = value;
                OnPropertyChanged();
            }
        }

        public SimulationObjectViewModel? SelectedBall
        {
            get => _selectedBall;
            set
            {
                _selectedBall = value;
                OnPropertyChanged();
            }
        }
        
        public double Gravity
        {
            get => _gravity;
            set
            {
                _gravity = value;
                _engine.EarthG = value;
                OnPropertyChanged();
            }
        }
        
        public double AirResistance
        {
            get => _airResistance;
            set
            {
                _airResistance = value;
                _engine.AirResistance = value;
                OnPropertyChanged();
            }
        }
        
        public double Friction
        {
            get => _friction;
            set
            {
                _friction = value;
                _engine.Friction = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand AddBallCommand { get; }
        public ICommand RemoveBallCommand { get; }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _engine.StepSimulation();
            
            // Update trajectory data
            foreach (var obj in _engine.Objects)
            {
                obj.UpdateBall();
            }
            
            UpdateSimulationObjects();
        }

        private void StartSimulation()
        {
            if (!IsSimulationRunning)
            {
                _timer.Start();
                IsSimulationRunning = true;
            }
        }

        private void StopSimulation()
        {
            if (IsSimulationRunning)
            {
                _timer.Stop();
                IsSimulationRunning = false;
            }
        }

        private void ResetSimulation()
        {
            StopSimulation();
            _engine.ResetSimulation();
            SimulationObjects.Clear();

            switch (_selectedSimulationType)
            {
                case SimulationType.ProjectileMotion:
                    SetupProjectileSimulation();
                    break;
                case SimulationType.PlanetaryOrbit:
                    SetupPlanetarySimulation();
                    break;
            }

            UpdateSimulationObjects();
        }

        private void SetupProjectileSimulation()
        {
            // Add initial ball
            AddProjectileBall();
            
            // Select the first ball by default
            if (SimulationObjects.Count > 0)
            {
                SelectedBall = SimulationObjects[0];
            }
        }
        
        private void AddProjectileBall()
        {
            double angleRadians = LaunchAngle * Math.PI / 180;
            Vector velocity = new(
                InitialVelocity * Math.Cos(angleRadians),
                InitialVelocity * Math.Sin(angleRadians)
            );
            
            int index = _engine.Objects.Count;
            string name = $"Ball {index + 1}";
            
            // Increase the ball radius from 0.5 to 10.0 for better visibility
            var projectile = new Projectile(1, new Vector(0, InitialHeight), velocity, 10.0, name);
            
            // Assign a color based on index
            if (index < _ballColors.Count)
            {
                projectile.Ball.Color = new SolidColorBrush(_ballColors[index]);
            }
            
            _engine.AddObject(projectile);
            SimulationObjects.Add(new SimulationObjectViewModel(projectile));
        }

        private void SetupPlanetarySimulation()
        {
            var centralPlanet = new Planet(PlanetMass, new Vector(0, 0), new Vector(0, 0), 20, "Planet");
            _engine.AddObject(centralPlanet);
            
            var satellite = new Planet(
                SatelliteMass, 
                new Vector(OrbitRadius, 0), 
                new Vector(0, OrbitVelocity), 
                5, 
                "Satellite"
            );
            
            // Set different colors
            centralPlanet.Ball.Color = new SolidColorBrush(Colors.Blue);
            satellite.Ball.Color = new SolidColorBrush(Colors.Green);
            
            _engine.AddObject(satellite);

            SimulationObjects.Add(new SimulationObjectViewModel(centralPlanet));
            SimulationObjects.Add(new SimulationObjectViewModel(satellite));
        }

        private void UpdateSimulationObjects()
        {
            for (int i = 0; i < _engine.Objects.Count; i++)
            {
                if (i < SimulationObjects.Count)
                {
                    SimulationObjects[i].Update(_engine.Objects[i]);
                }
            }
            
            // Update any changes made in the UI back to the physics objects
            if (SelectedBall != null && !IsSimulationRunning)
            {
                int index = SimulationObjects.IndexOf(SelectedBall);
                if (index >= 0 && index < _engine.Objects.Count)
                {
                    var obj = _engine.Objects[index];
                    obj.Mass = SelectedBall.Mass;
                    obj.Velocity = new Vector(SelectedBall.Vx, SelectedBall.Vy);
                    obj.Ball.Restitution = SelectedBall.Restitution;
                    obj.Radius = SelectedBall.Size / 2;
                    obj.Ball.Size = SelectedBall.Size;
                    obj.UpdateBall();
                }
            }
        }
        
        private bool CanAddBall()
        {
            return _engine.Objects.Count < _maxBalls && 
                   _selectedSimulationType == SimulationType.ProjectileMotion && 
                   !IsSimulationRunning;
        }
        
        private void AddBall()
        {
            if (CanAddBall())
            {
                AddProjectileBall();
            }
        }
        
        private bool CanRemoveBall()
        {
            return _engine.Objects.Count > 1 && 
                   _selectedSimulationType == SimulationType.ProjectileMotion && 
                   !IsSimulationRunning;
        }
        
        private void RemoveBall()
        {
            if (CanRemoveBall())
            {
                int lastIndex = _engine.Objects.Count - 1;
                _engine.Objects.RemoveAt(lastIndex);
                SimulationObjects.RemoveAt(lastIndex);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class SimulationObjectViewModel : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _radius;
        private string _name = string.Empty;
        private SolidColorBrush _color = new SolidColorBrush(Colors.Black);
        private ObservableCollection<Point> _trajectoryPoints = new ObservableCollection<Point>();
        private double _mass = 1.0;
        private double _vx = 0.0;
        private double _vy = 0.0;
        private double _restitution = 0.8;
        private double _size = 20.0;

        public SimulationObjectViewModel(SimulationObject obj)
        {
            Update(obj);
        }

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public double Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        
        public SolidColorBrush Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<Point> TrajectoryPoints
        {
            get => _trajectoryPoints;
            set
            {
                _trajectoryPoints = value;
                OnPropertyChanged();
            }
        }
        
        public double Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                OnPropertyChanged();
            }
        }
        
        public double Vx
        {
            get => _vx;
            set
            {
                _vx = value;
                OnPropertyChanged();
            }
        }
        
        public double Vy
        {
            get => _vy;
            set
            {
                _vy = value;
                OnPropertyChanged();
            }
        }
        
        public double Restitution
        {
            get => _restitution;
            set
            {
                _restitution = value;
                OnPropertyChanged();
            }
        }
        
        public double Size
        {
            get => _size;
            set
            {
                _size = value;
                _radius = value / 2;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Radius));
            }
        }

        public void Update(SimulationObject obj)
        {
            X = obj.Position.X;
            Y = obj.Position.Y;
            Radius = obj.Radius;
            Name = obj.Name;
            Color = obj.Ball.Color;
            Mass = obj.Mass;
            Vx = obj.Velocity.X;
            Vy = obj.Velocity.Y;
            Restitution = obj.Ball.Restitution;
            Size = obj.Ball.Size;
            
            // Update trajectory points
            TrajectoryPoints.Clear();
            foreach (var point in obj.Ball.TrajectoryPoints)
            {
                TrajectoryPoints.Add(point);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
