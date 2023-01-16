namespace Nagule;

using Aeco;

public interface ICommandContext : IDataLayer<IComponent>, ICommandBus
{
}