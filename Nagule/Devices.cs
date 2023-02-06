namespace Nagule;

public static class Devices
{
    public static Guid ScreenId { get; } = Guid.NewGuid();
    public static Guid WindowId { get; } = Guid.NewGuid();
    public static Guid KeyboardId { get; } = Guid.NewGuid();
    public static Guid MouseId { get; } = Guid.NewGuid();
    public static Guid CursorId { get; } = Guid.NewGuid();
}