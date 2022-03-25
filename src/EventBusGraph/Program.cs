using System.CommandLine;

var argument = new Argument<string>("Solution", "Path to the solution file in your unity projects root folder");
var outputOption = new Option<string?>("--out", "Path to put the generated json into");
outputOption.AddAlias("-o");
var cmd = new RootCommand
{
    argument,
    outputOption
};

cmd.SetHandler((string pathToSolution, string? pathToOutput) =>
{
    Console.WriteLine(pathToSolution);
    Console.WriteLine(pathToOutput);
}, argument, outputOption);

cmd.Invoke(args);