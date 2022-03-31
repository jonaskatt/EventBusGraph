namespace EventBusGraph.Models;

public record FileOutputFormat(string EventType, string MethodName, string FilePath, string LineSpan, string AssemblyName, EventBusGraphConnectionDirection Direction);