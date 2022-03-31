using System.CommandLine;
using EventBusGraph;
using EventBusGraph.Models;
using Microsoft.Extensions.Logging;

var argument = new Argument<string>("Solution", "Path to the solution file in your unity projects root folder");
var outputOption = new Option<string?>("--out", "Path to put the generated json into");
outputOption.AddAlias("-o");
var cmd = new RootCommand
{
    argument,
    outputOption
};

cmd.SetHandler(async (string pathToSolution, string? pathToOutput) =>
{
    if (!File.Exists(pathToSolution))
    {
        Console.WriteLine($"Not found {pathToSolution}");
    }

    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    var logger = loggerFactory.CreateLogger<Program>();
    var solutionLoader = new SolutionLoader(pathToSolution, loggerFactory);

    var unityProject = await solutionLoader.LoadAndCompileSingleProject("Assembly-CSharp");
    if (unityProject == null)
        return;
    var compilation = await unityProject.GetCompilationAsync();
    if (compilation == null)
        return;

    var eventBusResolver = new EventbusResolver(solutionLoader.Solution!, loggerFactory);
    var publishSymbols = eventBusResolver.GetEventPublicationSymbols(compilation);
    var publishCallers = await eventBusResolver.GetCallers(publishSymbols);

    var subscribeSymbols = eventBusResolver.GetEventSubscriptionSymbols(compilation);
    var subscribeCallers = await eventBusResolver.GetCallers(subscribeSymbols);

    var publicationsWithEvents = await eventBusResolver.GetEventTypesUsedAsArgumentsForCalledMethods(publishCallers);
    var subscriptionsWithEvents = await eventBusResolver.GetEventTypesUsedAsArgumentsForCalledMethods(subscribeCallers);
    logger.LogInformation("Found {PublishCount} publications and {SubscribeCount} subscriptions", publicationsWithEvents.Count, subscriptionsWithEvents.Count);

    var formattedPublications = publicationsWithEvents.Select(t => Formatter.Format(t, true));
    var formattedSubscriptions = subscriptionsWithEvents.Select(t => Formatter.Format(t, false));

    var joined = formattedPublications.Concat(formattedSubscriptions);
    var graph = Formatter.JoinTypeNodes(joined);

    pathToOutput ??= "out.json";
    var fileFormatted = Formatter.FormatToFileOutput(graph);
    logger.LogInformation("Writing graph with {Nodes} nodes to {Path}", fileFormatted.Count, pathToOutput);
    Exporter.ExportToFile(fileFormatted, pathToOutput);
}, argument, outputOption);

await cmd.InvokeAsync(args);

return 0;