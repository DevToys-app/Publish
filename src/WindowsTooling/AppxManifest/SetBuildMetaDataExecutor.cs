using System.Xml.Linq;

namespace WindowsTooling.AppxManifest;

public class SetBuildMetaDataExecutor : AppxManifestEditExecutor<SetBuildMetaData>, IValueChangedExecutor
{
    public SetBuildMetaDataExecutor(XDocument manifest) : base(manifest)
    {
    }

    public event EventHandler<CommandValueChanged> ValueChanged;

    public override Task Execute(SetBuildMetaData command, CancellationToken cancellationToken = default)
    {
        if (Manifest.Root == null)
        {
            throw new InvalidOperationException("The path must be a registry path.");
        }

        (string _, XNamespace buildNamespace) = EnsureNamespace(Namespaces.Build);

        XElement? metaData = Manifest.Root.Element(buildNamespace + "Metadata");
        if (metaData == null)
        {
            metaData = new XElement(buildNamespace + "Metadata");
            Manifest.Root.Add(metaData);
        }

        foreach (KeyValuePair<string, string> value in command.Values)
        {
            XElement? node = metaData.Elements(buildNamespace + "Item").FirstOrDefault(item => string.Equals(item.Attribute("Name")?.Value, value.Key, StringComparison.OrdinalIgnoreCase));
            if (node == null)
            {
                node = new XElement(buildNamespace + "Item");
                node.SetAttributeValue("Name", value.Key);
                metaData.Add(node);

                ValueChanged?.Invoke(this, new CommandValueChanged(value.Key, value.Value));
            }
            else
            {
                XAttribute? attr = node.Attribute("Version");
                if (attr != null && command.OnlyCreateNew)
                {
                    continue;
                }

                if (attr != null)
                {
                    ValueChanged?.Invoke(this, new CommandValueChanged(value.Key, attr.Value, value.Value));
                }
                else
                {
                    ValueChanged?.Invoke(this, new CommandValueChanged(value.Key, value.Value));
                }
            }

            node.SetAttributeValue("Version", value.Value);
        }

        return Task.CompletedTask;
    }
}
