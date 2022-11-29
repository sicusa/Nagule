namespace Nagule;

public interface IProfileListener
{
    void OnProfile(object layer, in LayerProfile profile);
}