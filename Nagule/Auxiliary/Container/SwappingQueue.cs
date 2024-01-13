namespace Nagule;

public class SwappingQueue<T>
{
    private readonly List<T> _list1 = [];
    private readonly List<T> _list2 = [];
    private bool _swapTag;
    private readonly object _sync = new();

    public List<T> Swap()
    {
        lock (_sync) {
            _swapTag = !_swapTag;
            return _swapTag ? _list2 : _list1;
        }
    }

    public void Add(in T value)
    {
        lock (_sync) {
            (_swapTag ? _list1 : _list2).Add(value);
        }
    }
}