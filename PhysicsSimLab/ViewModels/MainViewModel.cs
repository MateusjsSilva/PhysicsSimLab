using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
        
        private double _initialHeight = 10;
        private double _initialVelocity = 15;
        private double _launchAngle = 45;
        
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
            
            SimulationObjects = new ObservableCollection<SimulationObjectViewModel>();
            
            SelectedSimulationType = SimulationType.ProjectileMotion;
            ResetSimulation();
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

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ToggleSidebarCommand { get; }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _engine.StepSimulation();
            
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
            double angleRadians = LaunchAngle * Math.PI / 180;
            Vector velocity = new(
                InitialVelocity * Math.Cos(angleRadians),
                InitialVelocity * Math.Sin(angleRadians)
            );
            var projectile = new Projectile(1, new Vector(0, InitialHeight), velocity, 0.5);
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

        public void Update(SimulationObject obj)
        {
            X = obj.Position.X;
            Y = obj.Position.Y;
            Radius = obj.Radius;
            Name = obj.Name;
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
