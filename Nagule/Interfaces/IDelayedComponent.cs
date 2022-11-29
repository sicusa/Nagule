namespace Nagule;

public interface IDelayedComponent : IPooledComponent
{
    bool Dirty { get; set; }
}