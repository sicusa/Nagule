namespace Nagule;

public struct Profile
{
    public long InitialFrame;
    public double InitialTime;
    public double InitialElapsedTime;

    public double CurrentElapsedTime;
    public long CurrentFrame;
    public long CurrentTime;

    public double AverangeElapsedTime;
    public double MaximumElapsedTime;
    public double MinimumElapsedTime;
}