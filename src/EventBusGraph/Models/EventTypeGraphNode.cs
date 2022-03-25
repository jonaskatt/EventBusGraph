namespace EventBusGraph.Models;

public record EventTypeGraphNode(string EventType, IEnumerable<EventTypeGraphConnection> Connections);