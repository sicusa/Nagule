namespace Nagule;

[Flags]
public enum TransformDirtyTags
{
    None = 0,
    Local = 1,
    World = 2,
    View = 4,
    WorldComps = 8,
    NotifyEvent = 16,

    Globals = World | View | WorldComps | NotifyEvent,
    All = Local | Globals
}