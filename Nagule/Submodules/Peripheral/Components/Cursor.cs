namespace Nagule;

using Sia;

public partial record struct Cursor(
    [Sia] CursorState State,
    [Sia] CursorStyle Style);