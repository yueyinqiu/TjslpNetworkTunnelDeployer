using CliFx.Infrastructure;
using CliWrap;
using CliWrap.Buffered;

namespace TjslpNetworkTunnelDeployer.Extensions;

internal static class CommandExtensions
{
    extension(Command command)
    {
        public void Print(IConsole? console = null)
        {
            if (console is null)
            {
                Console.WriteLine($"> {command}");
            }
            else
            {
                console.WriteLine($"> {command}");
            }
        }

        public CommandTask<BufferedCommandResult> PrintAndExecuteBufferedAsync(IConsole? console = null)
        {
            command.Print(console);
            return command.ExecuteBufferedAsync();
        }
    }
}
