using Terraria.NetWork.Concept;

namespace Terraria.NetWork.Concept.GraphExporter;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args is not ["--output", var outputDirectory])
        {
            Console.Error.WriteLine("Usage: NetWork.Concept.GraphExporter --output <directory>");
            return 2;
        }

        GraphCodecEmitter.Export(PacketGraphCatalog.Export(), outputDirectory);
        return 0;
    }
}
