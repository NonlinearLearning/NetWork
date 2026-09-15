using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;

namespace Terraria.NetWork.Verification.LegacyTrHostWorker;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var options = ParseArgs(args);
            var protocolOut = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(TextWriter.Null);

            var serializer = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };

            var bootstrap = new LegacyRuntimeBootstrap(options.LegacyAssemblyPath, options.LegacyTempPath);
            var bridge = new LegacyObservationBridge(bootstrap, new LegacyFixtureRegistry());

            string? line;
            while ((line = Console.In.ReadLine()) is not null)
            {
                LegacyHostResponse response;
                try
                {
                    var request = serializer.Deserialize<LegacyHostRequest>(line)
                        ?? throw new InvalidOperationException("Request JSON deserialized to null.");
                    response = bridge.Execute(request);
                }
                catch (Exception ex)
                {
                    response = new LegacyHostResponse
                    {
                        Ok = false,
                        Error = ex.ToString(),
                        WorkerProcessId = Process.GetCurrentProcess().Id
                    };
                }

                protocolOut.WriteLine(serializer.Serialize(response));
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static WorkerOptions ParseArgs(string[] args)
    {
        string? assemblyPath = null;
        string? tempPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--assemblyPath":
                    assemblyPath = args[++i];
                    break;
                case "--tempPath":
                    tempPath = args[++i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(assemblyPath) || string.IsNullOrWhiteSpace(tempPath))
        {
            throw new InvalidOperationException("Missing required arguments: --assemblyPath and --tempPath");
        }

        return new WorkerOptions(assemblyPath!, tempPath!);
    }

    private sealed class WorkerOptions
    {
        public WorkerOptions(string legacyAssemblyPath, string legacyTempPath)
        {
            LegacyAssemblyPath = legacyAssemblyPath;
            LegacyTempPath = legacyTempPath;
        }

        public string LegacyAssemblyPath { get; }

        public string LegacyTempPath { get; }
    }
}
