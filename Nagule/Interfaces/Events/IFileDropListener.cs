namespace Nagule;

public interface IFileDropListener
{
    void OnFileDrop(IContext context, string[] fileNames);
}