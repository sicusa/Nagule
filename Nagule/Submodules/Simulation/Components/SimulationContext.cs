namespace Nagule;

using Sia;

public partial record struct SimulationContext()
{
    [Sia] public double? UpdateFrequency;
}