namespace Nagule;

public struct LayerProfile
{
    public double InitialElapsedTime;
    public long InitialUpdateFrame;
    public long InitialRenderFrame;

    public double CurrentElapsedTime;
    public long CurrentUpdateFrame;
    public long CurrentRenderFrame;

    public double AverangeElapsedTime;
    public double MaximumElapsedTime;
    public double MinimumElapsedTime;
}
