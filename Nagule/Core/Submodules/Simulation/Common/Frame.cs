namespace Nagule;

using Sia;

public abstract class Frame : IAddon
{
    public long FrameCount { get; private set; }
    public float Time { get; private set; }
    public float DeltaTime { get; private set; }

    public void Update(float deltaTime)
    {
        DeltaTime = deltaTime;
        Time += DeltaTime;
        OnTick();
        FrameCount++;
    }

    public virtual void OnInitialize(World world) {}
    public virtual void OnUninitialize(World world) {}

    protected abstract void OnTick();
}