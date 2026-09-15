using Terraria.NetWork.Verification.ProtocolDemoStandalone;

namespace Terraria.NetWork.Prototype.GraphLayoutCodegen;

internal static class Program
{
    private static int Main()
    {
        GraphExportTests.Run();
        ItemTweakerGraphPacketTests.Run();
        Packet20GraphExportTests.Run();
        PlayerActiveGraphPacketTests.Run();
        PlayerControlsGraphPacketTests.Run();
        QuikGraphDependencyTests.Run();
        Console.WriteLine("Graph layout/codegen verification passed.");
        return 0;
    }
}
