using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.06 DependencyGraph — tool depends on its icon; E010 missing, E011 cycle, E012 has-dependents.
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _edges = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nodes = new(StringComparer.Ordinal);

    public void AddNode(string id) => _nodes.Add(id);

    public void AddEdge(string from, string to)
    {
        _nodes.Add(from);
        _nodes.Add(to);
        if (!_edges.TryGetValue(from, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _edges[from] = set;
        }
        set.Add(to);
    }

    public IReadOnlyCollection<string> Nodes => _nodes;

    public IEnumerable<string> DependenciesOf(string id)
        => _edges.TryGetValue(id, out var set) ? set : Enumerable.Empty<string>();

    public IEnumerable<string> DependentsOf(string id)
        => _edges.Where(kv => kv.Value.Contains(id)).Select(kv => kv.Key);

    /// <summary>E010 if any edge target is not a known node.</summary>
    public List<(string From, string To)> MissingTargets()
    {
        var list = new List<(string, string)>();
        foreach (var (from, targets) in _edges)
        {
            foreach (var to in targets)
            {
                if (!_nodes.Contains(to))
                    list.Add((from, to));
            }
        }
        return list;
    }

    /// <summary>E011 — DFS cycle; returns path if found.</summary>
    public List<string>? FindCycle()
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();

        foreach (var n in _nodes.OrderBy(x => x, StringComparer.Ordinal))
        {
            var cycle = Dfs(n, visiting, visited, stack);
            if (cycle is not null) return cycle;
        }
        return null;
    }

    private List<string>? Dfs(string node, HashSet<string> visiting, HashSet<string> visited, List<string> stack)
    {
        if (visited.Contains(node)) return null;
        if (visiting.Contains(node))
        {
            var idx = stack.IndexOf(node);
            var path = stack.Skip(idx).ToList();
            path.Add(node);
            return path;
        }

        visiting.Add(node);
        stack.Add(node);
        if (_edges.TryGetValue(node, out var targets))
        {
            foreach (var t in targets.OrderBy(x => x, StringComparer.Ordinal))
            {
                var cycle = Dfs(t, visiting, visited, stack);
                if (cycle is not null) return cycle;
            }
        }
        stack.RemoveAt(stack.Count - 1);
        visiting.Remove(node);
        visited.Add(node);
        return null;
    }

    /// <summary>E012 — list dependents if removing/disabling id.</summary>
    public List<string> HasDependents(string id)
        => DependentsOf(id).OrderBy(x => x, StringComparer.Ordinal).ToList();
}
