namespace Nagule;

using Sia;

public partial record struct Cursor(
    [SiaProperty] CursorState State,
    [SiaProperty] CursorStyle Style);