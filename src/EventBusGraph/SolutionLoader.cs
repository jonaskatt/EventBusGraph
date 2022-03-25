using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using RendleLabs.AdhocWorkspaceLoader;

namespace EventBusGraph;

public class SolutionLoader
{
    private readonly string _pathToSolution;
    private readonly ILogger<SolutionLoader> _logger;
    private Dictionary<Project, Compilation> _compilations;
    private AdhocWorkspace? _workspace;

    public SolutionLoader(string pathToSolution, ILoggerFactory loggerFactory)
    {
        _pathToSolution = pathToSolution;
        _logger = loggerFactory.CreateLogger<SolutionLoader>();
        _compilations = new Dictionary<Project, Compilation>();
    }

    public Solution? Solution => _workspace?.CurrentSolution;
    
    public async Task LoadAndCompileProjects()
    {
        // RendleLabs legacy loader lets us load projects if they are using old .csproj structure
        // And our unity projects do just that
        var loader = new WorkspaceLoader();
        _workspace ??= await loader.LoadAsync(_pathToSolution);
        _logger.LogInformation("Loaded solution {Solution}", _workspace.CurrentSolution.FilePath);
        
        foreach (var item in _workspace.CurrentSolution.Projects)
        {
            var compilation = await item.GetCompilationAsync();
            if (compilation == null)
            {
                _logger.LogWarning("Compilation failed for {Project} and it will not be included in the final graph", item.Name);
                continue;
            }
            _compilations.Add(item, compilation);
            _logger.LogDebug("Compilation complete for {Project}", item.Name);
        }
    }

    public async Task<Project?> LoadAndCompileSingleProject(string projectName)
    {
        // RendleLabs legacy loader lets us load projects if they are using old .csproj structure
        // And our unity projects do just that
        var loader = new WorkspaceLoader();
        _workspace ??= await loader.LoadAsync(_pathToSolution);
        _logger.LogInformation("Loaded solution {Solution}", _workspace.CurrentSolution.FilePath);

        var project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => p.Name == projectName);
        if (project != null)
            return project;
        
        _logger.LogWarning("Could not find project {Name}", projectName);
        return null;
    }
}