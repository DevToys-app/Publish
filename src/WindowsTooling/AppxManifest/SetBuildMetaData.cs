namespace WindowsTooling.AppxManifest;

public class SetBuildMetaData : IAppxEditCommand
{
    public SetBuildMetaData(IDictionary<string, string> values)
    {
        Values = values ?? new Dictionary<string, string>();
    }

    public SetBuildMetaData(string key, string value)
    {
        Values = new Dictionary<string, string>
        {
            { key, value }
        };
    }

    public SetBuildMetaData(IReadOnlyDictionary<string, Version> versionComponents)
    {
        Values = versionComponents.ToDictionary(vc => vc.Key, vc => vc.Value.ToString());
    }

    public SetBuildMetaData(string component, Version version) : this(component, version.ToString())
    {
    }

    public IDictionary<string, string> Values { get; }

    public bool OnlyCreateNew { get; set; }
}
