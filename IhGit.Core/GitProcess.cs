using System.Diagnostics;
using System.Text;

namespace IhGit.Core;

public sealed record GitResult(int ExitCode, string Output, string Error)
{
    public bool Success => ExitCode == 0;

    public string Message => string.IsNullOrWhiteSpace(Error) ? Output : Error;
}

/// <summary>Exact git command lines shared by the application and the tests.</summary>
public static class GitCommands
{
    public static string[] CheckoutNewBranchFromOrigin(string newBranch, string remoteBranch)
        => ["checkout", "-b", newBranch, $"origin/{remoteBranch}", "--no-track"];

    public static string[] Checkout(string branch) => ["checkout", branch];

    public static string[] Push(string branch) => ["push", "-u", "origin", branch];

    public static string[] CherryPickContinue => ["cherry-pick", "--continue"];

    public static string[] CherryPickAbort => ["cherry-pick", "--abort"];

    public static string[] AddAll => ["add", "."];
}

public static class GitProcess
{
    public static GitResult Run(string workingDirectory, IEnumerable<string> arguments, IReadOnlyDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        info.Environment["GIT_TERMINAL_PROMPT"] = "0";

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
                info.Environment[key] = value;
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start git");
        process.StandardInput.Close();

        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        return new GitResult(process.ExitCode, output.Result.TrimEnd(), error.Result.TrimEnd());
    }

    public static string RunOrThrow(string workingDirectory, IEnumerable<string> arguments, IReadOnlyDictionary<string, string>? environment = null)
    {
        var args = arguments.ToArray();
        var result = Run(workingDirectory, args, environment);

        if (!result.Success)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed ({result.ExitCode}): {result.Message}");

        return result.Output;
    }
}
