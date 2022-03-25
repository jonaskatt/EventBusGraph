namespace EventBusGraph.Models;

public record EventTypeGraphConnection(string ClassName, string MethodName, string FilePath, string LineSpan, string AssemblyName, EventBusGraphConnectionDirection Direction);