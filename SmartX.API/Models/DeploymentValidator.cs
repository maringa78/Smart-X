namespace SmartX.Api.Models;

/// <summary>A node in the deployment hierarchy</summary>
public class ConfigNode
{
    public string Name { get; set; } = string.Empty;
    public List<ConfigNode> Children { get; set; } = new();
}

/// <summary>
/// Recursively validates that a "SubZoneB/Zone1/FacilityA" style path exists
/// within a known configuration tree, one segment at a time.
/// </summary>
public static class DeploymentValidator
{
    public static bool ValidatePath(ConfigNode root, string deploymentLocation)
    {
        var segments = deploymentLocation.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return ValidateNode(root, segments, 0);
    }

    private static bool ValidateNode(ConfigNode node, string[] pathSegments, int index)
    {
        // Base case: every segment has matched successfully
        if (index >= pathSegments.Length) return true;

        var next = node.Children.FirstOrDefault(
            c => c.Name.Equals(pathSegments[index], StringComparison.OrdinalIgnoreCase));

        if (next is null) return false; // base case: no matching child, path is invalid

        // Recursive case: descend one level and check the remaining segments
        return ValidateNode(next, pathSegments, index + 1);
    }
}