namespace Nagule;

using Sia;

public abstract class Frame : IAddon
{
    public long FrameCount => _frameCount;
    public float Time { get; private set; }
    public float DeltaTime { get; private set; }

    private long _frameCount;

    public void Update(float deltaTime)
    {
        DeltaTime = deltaTime;
        Time += DeltaTime;
        OnTick();
        Interlocked.Increment(ref _frameCount);
    }

    public virtual void OnInitialize(World world) {}
    public virtual void OnUninitialize(World world) {}

    protected abstract void OnTick();
}