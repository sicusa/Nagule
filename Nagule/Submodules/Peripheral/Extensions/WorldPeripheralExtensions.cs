namespace Nagule;

using Sia;

public static class WorldPeripheralExtensions
{
    public static Peripheral GetPeripheral(this World world)
        => world.GetAddon<Peripheral>();
}