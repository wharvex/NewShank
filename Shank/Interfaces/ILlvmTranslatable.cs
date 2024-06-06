namespace Shank.Interfaces;

public interface ILlvmTranslatable
{
    public string Name { get; set; }
    public string GetNameForLlvm() =>
        Name switch
        {
            "write" => "printf",
            "start" => "main",
            _ => Name
        };
}
