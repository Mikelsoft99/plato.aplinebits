namespace AlpineBits.GuestRequestProxy.Models.AlpineBits.Capabilities;


public class CapabilitiesConstants
{
    public readonly string SupportedVersion = "2024-10";

}


public class Capabilities
{
    public Version[] versions { get; set; }
    public static Capabilities MapString(string versionString)
    {
        return System.Text.Json.JsonSerializer.Deserialize<Capabilities>(versionString)!;
    }

    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}

public class Version
{
    public string version { get; set; }
    public Action[] actions { get; set; }

    public Version MatchInput(Version inputVersion)
    {
        Version output = new();
        output.version = inputVersion.version;

        foreach (var singleAction in actions)
        {
            // serach in inputVersion
            var matchedAction = inputVersion.actions.FirstOrDefault(a => a.action == singleAction.action);

            if (matchedAction != null)
            {
                var supportedFeatures = singleAction.supports?.Intersect(matchedAction.supports).ToArray() ?? [];
                if (supportedFeatures.Length > 0)
                {
                    output.actions = output.actions ?? Array.Empty<Action>();
                    output.actions = output.actions.Append(new Action
                    {
                        action = singleAction.action,
                        supports = supportedFeatures
                    }).ToArray();
                }
                else
                {
                    output.actions = output.actions ?? Array.Empty<Action>();
                    output.actions = output.actions.Append(new Action
                    {
                        action = singleAction.action,
                        supports = []
                    }).ToArray();
                }


            }
        }

        return output;

    }
}

public class Action
{
    public string action { get; set; }
    public string[] supports { get; set; } = new string[] { };
}
