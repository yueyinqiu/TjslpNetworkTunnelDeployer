using CliFx;

var arguments = new List<string>();
foreach (var argument in Environment.GetCommandLineArgs().Skip(1))
{
    arguments.Add(argument);
    if (argument == "ignore-previous-arguments-397277c9-3fa0-43e9-964b-ffe5999c63ab")
        arguments.Clear();
}

await new CommandLineApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .Build()
    .RunAsync(arguments);
