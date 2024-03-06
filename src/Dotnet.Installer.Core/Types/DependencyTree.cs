using Dotnet.Installer.Core.Models;

namespace Dotnet.Installer.Core.Types;

public class DependencyTree
{
    private readonly Dictionary<string, Component> _components = new();
    
    public DependencyTree() { }

    public DependencyTree(IEnumerable<Component> components)
    {
        foreach (var component in components) _components[component.Key] = component;
    }

    public void Add(Component component)
    {
        _components[component.Key] = component;
    }

    public List<Component> GetReverseDependencies(string startKey)
    {
        List<Component> reverseDependencies = [];
        HashSet<string> visited = [];

        TraverseReverseDependencies(startKey, reverseDependencies, visited);
        return reverseDependencies;
    }

    private void TraverseReverseDependencies(string key, 
        ICollection<Component> reverseDependencies,
        ICollection<string> visited)
    {
        if (!_components.TryGetValue(key, out var currentComponent) || visited.Contains(key))
            return;

        visited.Add(key);
        
        foreach (var kvp in _components.Where(kvp => 
                     kvp.Value.Dependencies.Contains(key)))
        {
            TraverseReverseDependencies(kvp.Key, reverseDependencies, visited);
            reverseDependencies.Add(kvp.Value);
        }
    }
}