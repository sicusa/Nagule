namespace Nagule;

public interface ICommand : IDisposable
{
    void Execute(IContext context);
}