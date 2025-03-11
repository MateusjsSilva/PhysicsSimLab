# PhysicsSimLab

PhysicsSimLab is an educational tool designed to help students and teachers visualize and experiment with basic physics concepts. The application simulates the motion of objects under gravity, with features for examining trajectories, collisions, and energy transformations.

## Features

- **Multiple Balls**: Add up to 5 different balls with varying properties
- **Real-time Parameter Adjustments**: Change gravity, air resistance, friction, mass, and restitution coefficient
- **Energy Analysis**: Real-time calculation of kinetic, potential, and total energy
- **Trajectory Tracking**: Visual path tracking for each object
- **Realistic Collisions**: Implements collision physics with deformation effects
- **Interactive UI**: Drag and place balls, zoom with Ctrl+Mouse Wheel

## Physics Models Implemented

- Projectile motion with air resistance
- Ground collisions with coefficient of restitution
- Friction during collision
- Energy conservation principles
- Size and mass effects on motion

## How to Use

1. **Starting the Simulation**: Click "Start" or use the Simulation menu
2. **Adding Balls**: Click the "+" button to add new balls (up to 5)
3. **Setting Parameters**:
   - Adjust velocity components (Vx, Vy)
   - Set physical properties (mass, size)
   - Modify environmental factors (gravity, air resistance)
4. **Interacting with Balls**: Drag balls to position them before starting the simulation
5. **Analyzing Results**: View the information panel for real-time data about position, velocity, and energy

## Physical Formulas Used

- **Motion Equations**:
  - x(t) = x₀ + v₀ₓ·t
  - y(t) = y₀ + v₀ᵧ·t - ½·g·t²
  - vᵧ(t) = v₀ᵧ - g·t

- **Collision Physics**:
  - vᵧ' = -vᵧ · CR (where CR is the restitution coefficient)
  - vₓ' = vₓ · FA (where FA is the horizontal friction factor)

- **Energy Calculations**:
  - E₍ₖ₎ = ½·m·v² (Kinetic energy)
  - E₍ₚ₎ = m·g·h (Potential energy)
  - E₍total₎ = E₍ₖ₎ + E₍ₚ₎ (Total energy)

## System Requirements

- Windows 10 or higher
- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code with C# extensions (for development)

## Building and Running

### Getting the Code
Clone the repository from GitHub:
```
git clone git@github.com:MateusjsSilva/PhysicsSimLab.git
```

### Using Visual Studio
1. Open the solution file in Visual Studio 2022
2. Build the solution (Ctrl+Shift+B)
3. Press F5 to run the application

### Using Command Line
1. Navigate to the project directory:
   ```
   cd PhysicsSimLab/PhysicsSimLab
   ```

2. Build the project:
   ```
   dotnet build
   ```

3. Run the application:
   ```
   dotnet run
   ```

4. For publishing a standalone application:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
   ```

## Project Structure

The project uses a service-oriented architecture:
- **Models**: Data structures like `BallData`
- **Services**: Core functionality including `PhysicsService`, `VisualizationService`, and `SimulationService` 
- **Helpers**: Utility classes like `MathHelper`
- **UI**: WPF interface in `MainWindow`

## Technical Details

- Built with C# and WPF
- Uses XAML for the user interface
- Implements physics through numerical integration

## Contribution

Feel free to open issues or submit pull requests. All contributions are welcome!

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

If you have any questions or suggestions, feel free to open an issue or submit a pull request!