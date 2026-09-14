namespace AgyToolbox.Core;

public interface ITool
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    Task RunAsync(string[] args);
}
