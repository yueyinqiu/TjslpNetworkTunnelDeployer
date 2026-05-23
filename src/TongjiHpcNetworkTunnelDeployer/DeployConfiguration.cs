using System.Text.Json;
using System.Text.Json.Serialization;
using static TjslpNetworkTunnelDeployer.DeployConfiguration;

namespace TjslpNetworkTunnelDeployer;

partial record DeployConfiguration(LocalToolConfiguration Tools, IReadOnlyList<TunnelConfiguration> Tunnels)
{
    public record ForwardConfiguration(int PortLocal, int PortLan);
    public record SingBoxConfiguration(int Port, string Username, string Password);
    public record SshConfiguration(string User, string Host, int Port);
    public record TunnelConfiguration(RemoteToolConfiguration Tools, SingBoxConfiguration SingBox, SshConfiguration Ssh, ForwardConfiguration Forward);
    public record LocalToolConfiguration(string SingBox, string Ssh);
    public record RemoteToolConfiguration(string SingBox);

    [JsonSerializable(typeof(DeployConfiguration))]
    [JsonSourceGenerationOptions(WriteIndented = true, IndentSize = 4)]
    private partial class DeployConfigurationSerializerContext : JsonSerializerContext
    {

    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, DeployConfigurationSerializerContext.Default.DeployConfiguration);
    }

    public static DeployConfiguration? Deserialize(string s)
    {
        return JsonSerializer.Deserialize(s, DeployConfigurationSerializerContext.Default.DeployConfiguration);
    }
}
