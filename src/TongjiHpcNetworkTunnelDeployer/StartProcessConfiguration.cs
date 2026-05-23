using CliFx.Binding;
using CliWrap;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TjslpNetworkTunnelDeployer.Extensions;

namespace TjslpNetworkTunnelDeployer;

partial record StartProcessConfiguration(
    int ParentProcess,
    string TargetFilePath, 
    IReadOnlyList<string> Arguments,
    string InputString,
    string OutputPath,
    string ErrorPath
)
{
    [JsonSerializable(typeof(StartProcessConfiguration))]
    [JsonSourceGenerationOptions(WriteIndented = true, IndentSize = 4)]
    private partial class StartProcessConfigurationSerializerContext : JsonSerializerContext
    {

    }

    public string ToBase64Url()
    {
        return Base64Url.EncodeToString(
            JsonSerializer.SerializeToUtf8Bytes(
                this, 
                StartProcessConfigurationSerializerContext.Default.StartProcessConfiguration
            )
        );
    }

    public static StartProcessConfiguration? FromBase64Url(string s)
    {
        return JsonSerializer.Deserialize(
            Base64Url.DecodeFromChars(s),
            StartProcessConfigurationSerializerContext.Default.StartProcessConfiguration
        );
    }

    public Command PrepareToRunStartProcessInNewProcess(
        string startProcessOutputPath, string startProcessErrorPath)
    {
        // TODO: Is there a better way?
        // https://stackoverflow.com/questions/79944193

        var targetFilePath = Environment.ProcessPath;
        Trace.Assert(targetFilePath is not null);

        var arguments = new List<string>()
        {
            Environment.GetCommandLineArgs()[0],
            "--",
            "ignore-previous-arguments-397277c9-3fa0-43e9-964b-ffe5999c63ab",
            "start-process",
            "--configuration-base64url", this.ToBase64Url()
        };
        return Cli.Wrap(targetFilePath)
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.Create(async (origin, cancellationToken) => {
                await origin.ToFileUnbufferedAsync(startProcessOutputPath, cancellationToken);
            }))
            .WithStandardErrorPipe(PipeTarget.Create(async (origin, cancellationToken) => {
                await origin.ToFileUnbufferedAsync(startProcessErrorPath, cancellationToken);
            }));
    }
}
