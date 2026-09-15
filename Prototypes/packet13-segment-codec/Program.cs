namespace Terraria.NetWork.Verification.PacketContextBindingPrototype;

internal static class Program
{
    private static readonly char[] DemoCommands = ['1', '2', 'v', '7', 's', 'l', 'b', 'p', 'o', 'm', 'r', 'i', 'z'];

    public static void Main(string[] args)
    {
        if (args.Any(argument =>
            argument.Equals("--demo", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("--script", StringComparison.OrdinalIgnoreCase)))
        {
            RunTranscript();
            return;
        }

        var engine = new PrototypeEngine();
        while (true)
        {
            Render(engine);
            var line = Console.ReadLine();
            if (line is null || line.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (line.Trim().Length == 0)
            {
                continue;
            }

            engine.Execute(line.Trim()[0]);
        }
    }

    private static void RunTranscript()
    {
        var engine = new PrototypeEngine();
        Render(engine, clear: false);
        foreach (var command in DemoCommands)
        {
            engine.Execute(command);
            Render(engine, clear: false);
        }
    }

    private static void Render(PrototypeEngine engine, bool clear = true)
    {
        if (clear && !Console.IsOutputRedirected)
        {
            Console.Clear();
        }

        Console.WriteLine("Packet context / bounded wire segment prototype");
        Console.WriteLine("================================================");
        Console.WriteLine();
        Console.WriteLine("[schema]");
        foreach (var specification in engine.RegisteredSegments)
        {
            Console.WriteLine($"  {specification}");
        }

        Console.WriteLine($"  registry: {(engine.RegistryIsFrozen ? "FROZEN" : "OPEN")}");
        Console.WriteLine();
        Console.WriteLine("[current state]");
        Console.WriteLine($"  action:          {engine.LastAction}");
        Console.WriteLine($"  result:          {engine.LastResult}");
        Console.WriteLine($"  final length:    {engine.FinalOutputLength}");
        Console.WriteLine($"  final bytes:     {engine.FinalOutputHex}");
        Console.WriteLine($"  read:            {engine.LastRead}");
        Console.WriteLine($"  external input:  {engine.LastExternal}");
        Console.WriteLine($"  submission:      {engine.LastSubmission}");
        Console.WriteLine($"  packet:          {engine.LastPacket}");
        Console.WriteLine($"  packet encodes:  {engine.PacketEncodeCount}");
        Console.WriteLine();
        Console.WriteLine("[commands]");
        Console.WriteLine("  1 fixed 4 bytes     2 variable 20 bytes     v variable 42 + read");
        Console.WriteLine("  7 variable 70 bytes s reject 19 bytes       l reject 71 bytes");
        Console.WriteLine("  b bad callback       p Packet 13 no optional  o Packet 13 optional");
        Console.WriteLine("  m mutate external input after project  r repeat Packet 13");
        Console.WriteLine("  i reject partial group  z reject late registration");
        Console.WriteLine("  c clear output       q quit");
    }
}
