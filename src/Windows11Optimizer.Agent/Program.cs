using System.Threading;

namespace Windows11Optimizer.Agent;

internal static class Program
{
    private const string MutexName =
        @"Local\Windows11Optimizer.Agent.Singleton";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(
            initiallyOwned: true,
            name: MutexName,
            createdNew: out var createdNew);

        if (!createdNew)
        {
            AgentApplicationContext.SignalExistingInstance(args);
            return;
        }

        ApplicationConfiguration.Initialize();

        using var context = new AgentApplicationContext();

        if (args.Any(x => x.Equals("--focus", StringComparison.OrdinalIgnoreCase)))
        {
            // La propia instancia registrará el hotkey; el usuario puede usarlo
            // inmediatamente. El panel se abre mediante una segunda señal.
            Task.Run(async () =>
            {
                await Task.Delay(250);
                AgentApplicationContext.SignalExistingInstance(["--focus"]);
            });
        }

        Application.Run(context);
        GC.KeepAlive(mutex);
    }
}
