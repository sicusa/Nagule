namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class OpenTKStyleUpdator : ViewBase
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in Cursor.SetState cmd) => {
            entity.Get<OpenTKWindow>().Native.CursorState = cmd.Value switch {
                CursorState.Normal => TKCursorState.Normal,
                CursorState.Hidden => TKCursorState.Hidden,
                CursorState.Grabbed => TKCursorState.Grabbed,
                _ => throw new InvalidDataException("Invalid cursor state")
            };
        });

        Listen((in EntityRef entity, in Cursor.SetStyle cmd) => {
            entity.Get<OpenTKWindow>().Native.Cursor = cmd.Value switch {
                CursorStyle.Default => MouseCursor.Default,
                CursorStyle.TextInput => MouseCursor.IBeam,
                CursorStyle.Crosshair => MouseCursor.Crosshair,
                CursorStyle.Hand => MouseCursor.Hand,
                CursorStyle.ResizeVertical => MouseCursor.VResize,
                CursorStyle.ResizeHorizontal => MouseCursor.HResize,
                CursorStyle.Empty => MouseCursor.Empty,
                _ => throw new InvalidDataException("Invalid cursor style")
            };
        });
    }
}