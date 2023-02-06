namespace Nagule;

using Aeco;

public interface ICommandHost : IDataLayer<IComponent>, ICommandBus
{
}