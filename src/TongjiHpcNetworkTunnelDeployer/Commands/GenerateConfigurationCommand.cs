using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using CliWrap;
using CliWrap.Buffered;
using System.Diagnostics;
using System.Net;
using System.Text;
using TjslpNetworkTunnelDeployer.Extensions;
using static TjslpNetworkTunnelDeployer.DeployConfiguration;

namespace TjslpNetworkTunnelDeployer.Commands;


[Command("generate-configuration", Description = "Generate the configuration file for tunnel deployment.")]
partial class GenerateConfigurationCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console)
    {
        console.Write("Local sing-box path (you can just type 'sing-box' if it is added to your PATH): ");
        var singBoxPath = console.ReadLine() ?? "";
        var singBoxVersion = await Cli.Wrap(singBoxPath).WithArguments("version")
            .PrintAndExecuteBufferedAsync(console);
        console.WriteLine(singBoxVersion.StandardOutput);
        console.WriteLine();
        console.WriteLine();

        console.Write("Local SSH path (you can just type 'ssh' if it is added to your PATH): ");
        var sshPath = console.ReadLine() ?? "";
        var sshVersion = await Cli.Wrap(sshPath).WithArguments(["-V"])
            .PrintAndExecuteBufferedAsync(console);
        console.WriteLine(sshVersion.StandardError);
        console.WriteLine();
        console.WriteLine();

        IReadOnlyList<string> addresses = (await Dns.GetHostAddressesAsync("logini.tongji.edu.cn"))
            .Concat(await Dns.GetHostAddressesAsync("logina.tongji.edu.cn"))
            .Select(x => x.ToString())
            .ToArray();
        console.WriteLine($"Detected login nodes: {string.Join(',', addresses)}");
        console.Write($"Please filter the login nodes (separate multiple IP addresses with commas): ");
        addresses = (console.ReadLine() ?? "").Split(',');
        console.WriteLine();
        console.WriteLine();

        console.Write($"Enter your hpc username: ");
        var username = console.ReadLine() ?? "";
        console.WriteLine();
        console.WriteLine();

        var tunnels = new List<TunnelConfiguration>();

        console.Write($"SSH will now attempt to connect to all nodes to add them to known_hosts. Please type 'yes' when prompted. Press Enter to continue: ");
        console.ReadLine();
        Encoding inputEncoding = Console.InputEncoding;
        Encoding outputEncoding = Console.OutputEncoding;

        var filteredAddresses = new List<string>();
        foreach (var address in addresses)
        {
            try
            {
                var command = Cli.Wrap(sshPath).WithArguments([
                    "-t",
                    "-p", "10022",
                    $"{username}@{address}",
                    $"echo \"Connected to {address}.\""
                ]);
                command.Print(console);
                await Process.Start(command.TargetFilePath, command.Arguments).WaitForExitAsync();
                filteredAddresses.Add(address);
            }
            catch
            {
            }
        }
        addresses = filteredAddresses;
        Console.InputEncoding = inputEncoding;
        Console.OutputEncoding = outputEncoding;
        console.WriteLine();
        console.WriteLine();

        var sshCommand = Cli.Wrap(sshPath);

        string? previousRemoteSingBoxPath = null;
        string previousRemoteSingBoxOutput = "";

        foreach (var address in addresses)
        {
            try
            {
                console.WriteLine($"Preparing configuration for {address}.");
                var output = await sshCommand
                    .WithArguments(["-p", "10022", $"{username}@{address}", "hostname"])
                    .PrintAndExecuteBufferedAsync(console);
                console.WriteLine($"Successfully connected to {address}. Hostname: {output.StandardOutput.Trim()}");
                console.Write($"Do you want to use {address}? (y/n): ");
                var yOrN = console.ReadLine() ?? "";
                if (!yOrN.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    console.WriteLine($"Skipped {address}.");
                    console.WriteLine();
                    console.WriteLine();
                    continue;
                }

                string? remoteSingBoxPath = null;
                if (previousRemoteSingBoxPath is not null)
                {
                    output = await sshCommand
                        .WithArguments(["-p", "10022", $"{username}@{address}", previousRemoteSingBoxPath, "version"])
                        .ExecuteBufferedAsync();
                    if (output.StandardOutput == previousRemoteSingBoxOutput)
                    {
                        console.WriteLine("Automatically reused the sing-box path from the previous node.");
                        remoteSingBoxPath = previousRemoteSingBoxPath;
                    }
                    else
                    {
                        console.WriteLine("The sing-box path from the previous node is not available here.");
                    }
                }

                if (remoteSingBoxPath is null)
                {
                    console.Write($"Remote sing-box path (you can just type 'sing-box' if it is added to PATH): ");
                    remoteSingBoxPath = console.ReadLine() ?? "";

                    output = await sshCommand
                        .WithArguments(["-p", "10022", $"{username}@{address}", remoteSingBoxPath, "version"])
                        .PrintAndExecuteBufferedAsync(console);
                    console.WriteLine(output.StandardOutput);
                    console.WriteLine();
                    console.WriteLine();
                    previousRemoteSingBoxPath = remoteSingBoxPath;
                    previousRemoteSingBoxOutput = output.StandardOutput;
                }

                tunnels.Add(new(
                    new RemoteToolConfiguration(remoteSingBoxPath),
                    new SingBoxConfiguration(
                        Random.Shared.Next(10000, 60000),
                        Guid.NewGuid().ToString("N"),
                        Guid.NewGuid().ToString("N")
                    ),
                    new SshConfiguration(username, address, 10022),
                    new ForwardConfiguration(Random.Shared.Next(10000, 60000), Random.Shared.Next(10000, 60000))
                ));
                console.WriteLine($"Configuration for {address} completed.");
                console.WriteLine();
                console.WriteLine();
            }
            catch (Exception ex)
            {
                console.Write(
                    $"""
                    An exception occurred:
                    {ex}

                    Skipped {address}.
                    """);
                console.WriteLine();
                console.WriteLine();
            }
        }

        var configuration = new DeployConfiguration(new(singBoxPath, sshPath), tunnels).Serialize();
        console.WriteLine("Configuration generated successfully:");
        console.WriteLine(configuration);

        console.Write("Save configuration to: ");
        var outputPath = console.ReadLine() ?? "";
        await File.WriteAllTextAsync(outputPath, configuration);
        console.WriteLine("Done.");
    }
}