using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public abstract class SecurityAndPerformanceSourceTestBase
{
    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

    protected static string ReadWebApiFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.WebApi", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadWebAdminFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.WebAdmin", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadApplicationFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Application", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadDomainFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Domain", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadInfrastructureFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Infrastructure", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadWorkerFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Worker", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadMobileBusinessFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Mobile.Business", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadMobileConsumerFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Mobile.Consumer", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadMobileSharedFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Mobile.Shared", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadTestProjectFile(string relativePath)
    {
        var path = ResolveRepositoryPath("tests", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ReadWebFrontendFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Web", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string NormalizeJsonKeyValueSpacing(string source)
    {
        return source.Replace("\":  ", "\": ");
    }

    protected static string ReadContractsFile(string relativePath)
    {
        var path = ResolveRepositoryPath("src", "Darwin.Contracts", relativePath);

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    protected static string ResolveRepositoryPath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine([RepositoryRoot.Value, .. segments]));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Darwin.sln"))
                && Directory.Exists(Path.Combine(directory.FullName, "src", "Darwin.WebAdmin")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Darwin repository root from '{AppContext.BaseDirectory}'.");
    }
}

