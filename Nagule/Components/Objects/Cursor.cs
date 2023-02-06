namespace Nagule;

public struct Cursor : IReactiveSingletonComponent
{
    public CursorState State = CursorState.Normal;
    public CursorStyle Style = CursorStyle.Default;

    public Cursor() {}
}