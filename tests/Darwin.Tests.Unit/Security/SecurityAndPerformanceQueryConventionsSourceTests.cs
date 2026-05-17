using System.Text.RegularExpressions;
using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceQueryConventionsSourceTests : SecurityAndPerformanceSourceTestBase
{
    private static readonly Regex LikeInvocationRegex = new(
        @"EF\.Functions\.Like\s*\((?<arguments>(?:[^()]+|\([^()]*\))*)\)",
        RegexOptions.Compiled);

    private static readonly Regex PredicateInvocationRegex = new(
        @"\.(?<method>Where|Any|All|AnyAsync|Count|CountAsync|First|FirstOrDefault|FirstOrDefaultAsync|Single|SingleOrDefault|SingleOrDefaultAsync|OrderBy|OrderByDescending|ThenBy|ThenByDescending)\s*\((?<body>(?:[^()]+|\([^()]*\))*)\)",
        RegexOptions.Compiled);

    [Fact]
    public void ApplicationQueries_Should_Use_EscapeCharacter_For_EfFunctionsLike()
    {
        var querySources = ReadApplicationQuerySources();
        var violatingCalls = querySources.SelectMany(
                source => FindLikeInvocationsWithoutEscape(source.Path, source.Source))
            .ToList();

        violatingCalls.Should().BeEmpty(
            "all application query EF.Functions.Like invocations should pass QueryLikePattern.EscapeCharacter; found:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violatingCalls.Select(BuildViolation)));
    }

    [Fact]
    public void ApplicationQueries_Should_Not_Use_EnumOrGuid_ToString_In_Query_Predicates()
    {
        var querySources = ReadApplicationQuerySources();
        var violatingCalls = querySources.SelectMany(
                source => FindPredicateCallsWithToString(source.Path, source.Source))
            .ToList();

        violatingCalls.Should().BeEmpty(
            "query predicates should avoid inline Enum/Guid.ToString() translation in SQL and use explicit status token matching or constants; found:"
            + Environment.NewLine + string.Join(Environment.NewLine, violatingCalls.Select(BuildViolation)));
    }

    [Fact]
    public void ApplicationQueries_Should_Not_Use_Moving_DateTimeUtcNow_In_Query_Predicates()
    {
        var querySources = ReadApplicationQuerySources();
        var violatingCalls = querySources.SelectMany(
                source => FindPredicateCallsWithMovingUtcNow(source.Path, source.Source))
            .ToList();

        violatingCalls.Should().BeEmpty(
            "query predicates should use local UTC snapshots/cutoff parameters instead of DateTime.UtcNow inside translated expressions; found:"
            + Environment.NewLine + string.Join(Environment.NewLine, violatingCalls.Select(BuildViolation)));
    }

    private static List<SourceViolation> FindLikeInvocationsWithoutEscape(string path, string source)
    {
        return LikeInvocationRegex.Matches(source)
            .Cast<Match>()
            .Where(match => match.Success
                && !match.Groups["arguments"].Value.Contains("QueryLikePattern.EscapeCharacter", StringComparison.Ordinal))
            .Select(match => new SourceViolation(
                path,
                GetLineNumber(source, match.Index),
                match.Value.Trim()))
            .ToList();
    }

    private static List<SourceViolation> FindPredicateCallsWithToString(string path, string source)
    {
        return PredicateInvocationRegex.Matches(source)
            .Cast<Match>()
            .Where(match => match.Groups["body"].Value.Contains(".ToString()", StringComparison.Ordinal))
            .Select(match => new SourceViolation(
                path,
                GetLineNumber(source, match.Index),
                match.Value.Trim()))
            .ToList();
    }

    private static List<SourceViolation> FindPredicateCallsWithMovingUtcNow(string path, string source)
    {
        return PredicateInvocationRegex.Matches(source)
            .Cast<Match>()
            .Where(match => match.Groups["body"].Value.Contains("DateTime.UtcNow", StringComparison.Ordinal))
            .Select(match => new SourceViolation(
                path,
                GetLineNumber(source, match.Index),
                match.Value.Trim()))
            .ToList();
    }

    private static int GetLineNumber(string source, int index)
    {
        return source[..index].Count('\n') + 1;
    }

    private static string BuildViolation(SourceViolation violation)
    {
        var message = violation.Snippet.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (message.Length > 220)
        {
            message = $"{message[..220]}...";
        }

        return $"- {violation.Path}:{violation.Line}: {message}";
    }

    private static (string Path, string Source)[] ReadApplicationQuerySources()
    {
        var root = ResolveRepositoryPath("src", "Darwin.Application");

        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(IsQuerySourceFile)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => (Path: Path.GetRelativePath(root, path), Source: File.ReadAllText(path)))
            .ToArray();
    }

    private static bool IsQuerySourceFile(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.EndsWith("Queries.cs", StringComparison.Ordinal);
    }

    private sealed record SourceViolation(string Path, int Line, string Snippet);
}
