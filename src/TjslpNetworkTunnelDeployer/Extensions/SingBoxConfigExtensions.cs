using SingBoxLib.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TjslpNetworkTunnelDeployer.Extensions;

internal static class SingBoxConfigExtensions
{
    private static readonly JsonSerializerOptions prettyJsonOptions = new() {
        WriteIndented = true,
        IndentSize = 4
    };

    extension(SingBoxConfig singBoxConfig)
    {
        public string ToJsonPretty()
        {
            var json = singBoxConfig.ToJson();
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, prettyJsonOptions);
        }
        public string ToBase64()
        {
            var json = singBoxConfig.ToJson();
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }
    }
}
