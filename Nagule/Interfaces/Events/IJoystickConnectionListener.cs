namespace Nagule;

public interface IJoystickConnectionListener
{
    void OnJoystickConnection(IContext context, int joystickId, bool connected);
}