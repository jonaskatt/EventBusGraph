using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;

namespace EventBusGraph;

public class EventbusResolver
{
    private readonly Solution _solution;
    private INamedTypeSymbol? _eventBusClassInfo;
    private readonly ILogger<EventbusResolver> _logger;

    public EventbusResolver(Solution solution, ILoggerFactory loggerFactory)
    {
        _solution = solution;
        _logger = loggerFactory.CreateLogger<EventbusResolver>();
    }
    
    public IList<ISymbol> GetEventPublicationSymbols(Compilation compilation)
    {
        _eventBusClassInfo ??= compilation.GetTypeByMetadataName("Core.Events.EventBus");
        if (_eventBusClassInfo == null)
        {
            _logger.LogWarning("Could not find Core.Events.EventBus class info. Selected project might not use eventbus");
            return new List<ISymbol>();
        }
        
        // Find all publish method symbols
        var publishMethods1 = _eventBusClassInfo.GetMembers("Publish");
        var publishMethods2 = _eventBusClassInfo.GetMembers("PublishExclusive");

        var all = publishMethods1.Concat(publishMethods2).ToList();
        
        _logger.LogDebug("Found {Count} symbols for publishing", all.Count);
        
        return all;
    }
    
    public IList<ISymbol> GetEventSubscriptionSymbols(Compilation compilation)
    {
        _eventBusClassInfo ??= compilation.GetTypeByMetadataName("Core.Events.EventBus");
        if (_eventBusClassInfo == null)
        {
            _logger.LogWarning("Could not find Core.Events.EventBus class info. Selected project might not use eventbus");
            return new List<ISymbol>();
        }
        
        // Find all subscribe method symbols
        var subscribeMethods1 = _eventBusClassInfo.GetMembers("Subscribe");
        var subscribeMethods2 = _eventBusClassInfo.GetMembers("SubscribeOnce");
        var subscribeMethods3 = _eventBusClassInfo.GetMembers("SubscribeUntilTrue");
        var subscribeMethods4 = _eventBusClassInfo.GetMembers("SetSubscribed");

        var all = subscribeMethods1.Concat(subscribeMethods2).Concat(subscribeMethods3).Concat(subscribeMethods4).ToList();
        
        _logger.LogDebug("Found {Count} symbols for subscribing", all.Count);
        
        return all;
    }

    public async Task<IList<SymbolCallerInfo>> GetCallers(IEnumerable<ISymbol> symbols)
    {
        var callers = new List<SymbolCallerInfo>();
        foreach (var symbol in symbols)
        {
             callers.AddRange(await SymbolFinder.FindCallersAsync(symbol, _solution));
        }

        return callers;
    }
    
    public async Task<List<(SymbolCallerInfo, ITypeSymbol)>> GetEventTypesUsedAsArgumentsForCalledMethods(IEnumerable<SymbolCallerInfo> callers)
    {
        var events = new List<(SymbolCallerInfo, ITypeSymbol)>();
        foreach (var caller in callers)
        {
            foreach (var location in caller.Locations)
            {
                if (!location.IsInSource)
                    continue;

                var document = _solution.GetDocument(location.SourceTree);
                if (document == null)
                    continue;
                    
                var semanticModel = await document.GetSemanticModelAsync();
                if (semanticModel == null)
                    continue;
                    
                var node = (await location.SourceTree.GetRootAsync()).FindToken(location.SourceSpan.Start).Parent;
                if (node == null)
                    continue;
                
                var symbolInfo = semanticModel.GetSymbolInfo(node);
                if (symbolInfo.Symbol is not IMethodSymbol calledMethod)
                    continue;

                if (calledMethod.TypeArguments.Length > 1)
                {
                    _logger.LogDebug("Encountered invocation with more than 1 type argument. Dont know how to handle that. Arguments are {Arguments}", calledMethod.TypeArguments);
                    continue;
                }
                
                events.Add((caller, calledMethod.TypeArguments[0]));
            }
        }

        return events;
    }
}