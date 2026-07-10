using YamlDotNet.RepresentationModel;

namespace cryptotracker.webapi.Configuration;

public static class YamlConfigurationExtensions
{
    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path, bool optional = false)
    {
        return builder.Add(new YamlConfigurationSource { Path = path, Optional = optional });
    }
}

public class YamlConfigurationSource : IConfigurationSource
{
    public required string Path { get; set; }
    public bool Optional { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new YamlConfigurationProvider(this);
}

public class YamlConfigurationProvider : ConfigurationProvider
{
    private readonly YamlConfigurationSource _source;

    public YamlConfigurationProvider(YamlConfigurationSource source)
    {
        _source = source;
    }

    public override void Load()
    {
        if (!File.Exists(_source.Path))
        {
            if (_source.Optional) return;
            throw new FileNotFoundException($"Config file not found: {_source.Path}");
        }

        using var reader = new StreamReader(_source.Path);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (yaml.Documents.Count > 0 && yaml.Documents[0].RootNode is YamlMappingNode root)
        {
            Flatten(root, "", data);
        }

        Data = data;
    }

    private static void Flatten(YamlNode node, string prefix, Dictionary<string, string?> data)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (var (key, value) in mapping.Children)
                {
                    var name = ((YamlScalarNode)key).Value ?? "";
                    Flatten(value, prefix.Length == 0 ? name : $"{prefix}:{name}", data);
                }
                break;
            case YamlSequenceNode sequence:
                for (var i = 0; i < sequence.Children.Count; i++)
                {
                    Flatten(sequence.Children[i], $"{prefix}:{i}", data);
                }
                break;
            case YamlScalarNode scalar:
                data[prefix] = scalar.Value;
                break;
        }
    }
}
