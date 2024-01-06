namespace Nagule;

using Sia;

public partial record struct SimulationContext()
{
    [SiaProperty] public double? UpdateFrequency;
}