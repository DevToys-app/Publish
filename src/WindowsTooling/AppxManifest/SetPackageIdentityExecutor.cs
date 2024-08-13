using Serilog;
using System.Xml.Linq;

namespace WindowsTooling.AppxManifest;

public class SetPackageIdentityExecutor : AppxManifestEditExecutor<SetPackageIdentity>, IValueChangedExecutor
{
    public SetPackageIdentityExecutor(XDocument manifest) : base(manifest)
    {
    }

    public event EventHandler<CommandValueChanged>? ValueChanged;

    public override Task Execute(SetPackageIdentity command, CancellationToken cancellationToken = default)
    {
        (string _, XNamespace rootNamespace) = EnsureNamespace(Namespaces.Root);
        XName identityFullName = rootNamespace + "Identity";


        XElement? identity = Manifest.Root?.Element(identityFullName);
        if (identity == null)
        {
            identity = new XElement(rootNamespace + "Identity");
            Manifest.Root?.Add(identity);
        }

        if (command.Publisher != null)
        {
            string? validationError = AppxValidatorFactory.ValidateSubject()(command.Publisher);
            if (validationError != null)
            {
                throw new ArgumentException(validationError, nameof(command));
            }

            XAttribute? attr = identity.Attribute("Publisher");
            if (attr == null)
            {
                Log.Information("Setting attribute 'Publisher' to '{0}'…", command.Publisher);
                attr = new XAttribute("Publisher", command.Publisher);
                identity.Add(attr);
                ValueChanged?.Invoke(this, new CommandValueChanged("Publisher", command.Publisher));
            }
            else
            {
                Log.Information("Changing attribute 'Publisher' from '{0}' to '{1}'…", attr.Value, command.Publisher);
                ValueChanged?.Invoke(this, new CommandValueChanged("Publisher", attr.Value, command.Publisher));
                attr.Value = command.Publisher;
            }
        }

        if (command.Name != null)
        {
            string? validationError = AppxValidatorFactory.ValidatePackageName()(command.Name);
            if (validationError != null)
            {
                throw new ArgumentException(validationError, nameof(command));
            }

            XAttribute? attr = identity.Attribute("Name");
            if (attr == null)
            {
                Log.Information("Setting attribute 'Name' to '{0}'…", command.Name);
                ValueChanged?.Invoke(this, new CommandValueChanged("Name", command.Name));
                attr = new XAttribute("Name", command.Name);
                identity.Add(attr);
            }
            else
            {
                Log.Information("Changing attribute 'Name' from '{0}' to '{1}'…", attr.Value, command.Name);
                ValueChanged?.Invoke(this, new CommandValueChanged("Name", attr.Value, command.Name));
                attr.Value = command.Name;
            }
        }

        if (command.Version != null)
        {
            XAttribute? attr = identity.Attribute("Version");
            if (attr == null)
            {
                string newVersion = VersionStringOperations.ResolveMaskedVersion(command.Version);
                string? validationError = AppxValidatorFactory.ValidateVersion()(newVersion);
                if (validationError != null)
                {
                    throw new ArgumentException(validationError, nameof(command));
                }

                Log.Information("Setting attribute 'Version' to '{0}'…", newVersion);
                attr = new XAttribute("Version", newVersion);
                identity.Add(attr);
            }
            else
            {
                string newVersion = VersionStringOperations.ResolveMaskedVersion(command.Version, attr.Value);

                string? validationError = AppxValidatorFactory.ValidateVersion()(newVersion);
                if (validationError != null)
                {
                    throw new ArgumentException(validationError, nameof(command));
                }

                Log.Information("Changing attribute 'Version' from '{0}' to '{1}'…", attr.Value, newVersion);
                ValueChanged?.Invoke(this, new CommandValueChanged("Version", attr.Value, newVersion));
                attr.Value = newVersion;
            }
        }

        if (command.ProcessorArchitecture != null)
        {
            XAttribute? attr = identity.Attribute("ProcessorArchitecture");
            if (attr == null)
            {
                Log.Information("Setting attribute 'ProcessorArchitecture' to '{0}'…", command.ProcessorArchitecture);
                ValueChanged?.Invoke(this, new CommandValueChanged("ProcessorArchitecture", command.ProcessorArchitecture));
                attr = new XAttribute("ProcessorArchitecture", command.ProcessorArchitecture);
                identity.Add(attr);
            }
            else
            {
                Log.Information("Changing attribute 'ProcessorArchitecture' from '{0}' to '{1}'…", attr.Value, command.ProcessorArchitecture);
                ValueChanged?.Invoke(this, new CommandValueChanged("ProcessorArchitecture", attr.Value, command.ProcessorArchitecture));
                attr.Value = command.ProcessorArchitecture;
            }
        }

        if (command.ResourceId != null)
        {
            string? validationError = AppxValidatorFactory.ValidateResourceId()(command.ResourceId);
            if (validationError != null)
            {
                throw new ArgumentException(validationError, nameof(command));
            }

            XAttribute? attr = identity.Attribute("ResourceId");
            if (attr == null)
            {
                Log.Information("Setting attribute 'ResourceId' to '{0}'…", command.ResourceId);
                ValueChanged?.Invoke(this, new CommandValueChanged("ResourceId", command.ResourceId));
                attr = new XAttribute("ResourceId", command.ResourceId);
                identity.Add(attr);
            }
            else
            {
                Log.Information("Changing attribute 'ResourceId' from '{0}' to '{1}'…", attr.Value, command.ResourceId);
                ValueChanged?.Invoke(this, new CommandValueChanged("ResourceId", attr.Value, command.ResourceId));
                attr.Value = command.ResourceId;
            }
        }

        return Task.CompletedTask;
    }

}
