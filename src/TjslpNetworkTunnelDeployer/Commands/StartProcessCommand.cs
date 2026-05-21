using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using CliWrap;
using System.Diagnostics;
using TjslpNetworkTunnelDeployer.Extensions;

namespace TjslpNetworkTunnelDeployer.Commands;

[Command("start-process", Description = "Used internally to safely start a child process.")]
public partial class StartProcessCommand : ICommand
{
    [CommandOption("configuration-base64url")]
    public required string ConfigurationBase64url { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var configuration = StartProcessConfiguration.FromBase64Url(ConfigurationBase64url);
        console.WriteLine(configuration);
        Trace.Assert(configuration is not null);

        var command = Cli.Wrap(configuration.TargetFilePath).WithArguments(configuration.Arguments);
        command.Print(console);
        using var process = Process.Start(
            new ProcessStartInfo(command.TargetFilePath, command.Arguments)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        );
        Debug.Assert(process is not null);

        var outputTasks = new List<Task>();
        try
        {
            using (var writer = new StreamWriter(process.StandardInput.BaseStream))
            {
                await writer.WriteAsync(configuration.InputString);
                await writer.FlushAsync();
            }

            outputTasks.Add(process.StandardOutput.BaseStream.ToFileUnbufferedAsync(configuration.OutputPath));
            outputTasks.Add(process.StandardError.BaseStream.ToFileUnbufferedAsync(configuration.ErrorPath));

            var parentProcess = Process.GetProcessById(configuration.ParentProcess);
            _ = await Task.WhenAny([.. outputTasks, parentProcess.WaitForExitAsync()]);
        }
        finally
        {
            try
            {
                process.Kill(true);
            }
            finally
            {
                await Task.WhenAll(outputTasks);
            }
        }
    }
}