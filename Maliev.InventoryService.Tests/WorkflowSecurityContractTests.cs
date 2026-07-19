using System.Text.RegularExpressions;
using Xunit;

namespace Maliev.InventoryService.Tests;

/// <summary>
/// Executable contracts for pull-request package authentication and workflow action integrity.
/// </summary>
public sealed class WorkflowSecurityContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>
    /// Dependabot cannot access repository secrets, so package restore must use the job token fallback.
    /// </summary>
    [Fact]
    public void PullRequestRestore_UsesDependabotSafePackageToken()
    {
        var caller = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "pr-validation.yml"))
            .ReplaceLineEndings("\n");
        var reusable = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "_build-and-test.yml"));

        Assert.Contains("permissions:\n  contents: read\n  packages: read", caller, StringComparison.Ordinal);
        Assert.Contains("gitops_pat: ${{ secrets.GITOPS_PAT || github.token }}", caller, StringComparison.Ordinal);
        Assert.DoesNotContain("gitops_pat: ${{ secrets.GITOPS_PAT }}", caller, StringComparison.Ordinal);
        Assert.Contains("NUGET_PASSWORD: ${{ secrets.gitops_pat }}", reusable, StringComparison.Ordinal);
    }

    /// <summary>
    /// Upgraded third-party workflow actions must remain pinned to immutable commits.
    /// </summary>
    [Fact]
    public void UpgradedActions_ArePinnedAcrossWorkflows()
    {
        var workflowDirectory = Path.Combine(RepositoryRoot, ".github", "workflows");
        var references = Directory.EnumerateFiles(workflowDirectory, "*.yml")
            .SelectMany(path => Regex.Matches(
                File.ReadAllText(path),
                "uses: (?:actions/(?:checkout|setup-dotnet)|imranismail/setup-kustomize)@([^\\s#]+)"))
            .Select(match => match.Groups[1].Value)
            .ToArray();

        Assert.NotEmpty(references);
        Assert.All(references, reference => Assert.Matches("^[0-9a-f]{40}$", reference));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maliev.InventoryService.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the InventoryService repository root.");
    }
}
