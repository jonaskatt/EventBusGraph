using EventBusGraph.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace EventBusGraph;

public static class Formatter
{
    public static Dictionary<string, List<FileOutputFormat>> FormatToFileOutput(IEnumerable<EventTypeGraphNode> nodes)
    {
        var output = new Dictionary<string, List<FileOutputFormat>>();
        foreach (var node in nodes)
        {
            foreach (var connection in node.Connections)
            {
                if (!output.ContainsKey(connection.ClassName))
                {
                    output.Add(connection.ClassName,
                        new List<FileOutputFormat>
                        {
                            new(node.EventType,
                                connection.MethodName,
                                connection.FilePath,
                                connection.LineSpan,
                                connection.AssemblyName,
                                connection.Direction)
                        });
                }
                else
                {
                    output[connection.ClassName].Add(new FileOutputFormat(node.EventType, connection.MethodName, connection.FilePath, connection.LineSpan, connection.AssemblyName, connection.Direction));
                }
            }
        }

        return output;
    }
    
    public static EventTypeGraphNode Format((SymbolCallerInfo, ITypeSymbol) element, bool isPublication)
    {
        var (caller, eventType) = element;
        var className = caller.CallingSymbol.ContainingType.Name;
        var methodName = caller.CallingSymbol.Name;
        var locations = caller.CallingSymbol.Locations;
        var location = locations[0];
        var path = locations.Select(l => l.GetLineSpan().Path).FirstOrDefault() ?? string.Empty;
        var assemblyName = caller.CallingSymbol.ContainingAssembly.Identity.Name;

        var direction = isPublication
            ? EventBusGraphConnectionDirection.Outgoing
            : EventBusGraphConnectionDirection.Incoming;

        return new EventTypeGraphNode(eventType.Name, new []
        {
            new EventTypeGraphConnection(className, methodName, path, location.GetLineSpan().Span.ToString(), assemblyName, direction)
        });
    }
    
    public static IEnumerable<EventTypeGraphNode> JoinTypeNodes(IEnumerable<EventTypeGraphNode> nodes)
    {
        var grouped = nodes.GroupBy(n => n.EventType, node => node);
        
        // Join all nodes that have the same event type to merge their list of connections
        foreach (var group in grouped)
        {
            yield return new EventTypeGraphNode(group.Key, group.SelectMany(n => n.Connections));
        }
    }
}