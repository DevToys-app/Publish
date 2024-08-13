namespace WindowsTooling.AppxManifest;

public class AddCapability : IAppxEditCommand
{
    public AddCapability()
    {
    }

    public AddCapability(string name)
    {
        Name = name;
    }

    public string? Name { get; set; }
}
