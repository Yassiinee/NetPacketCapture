
using Microsoft.Extensions.Configuration;
using Serilog;

namespace NetPacketCapture
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Log.Information("NetPacketCapture starting");
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║   NetPacketCapture - Packet Sniffer      ║");
            Console.WriteLine("║   Layer 2 Ethernet Frame Capture         ║");
            Console.WriteLine("╚══════════════════════════════════════════╝\n");

            // Check if running with appropriate permissions
            if (!PermissionHelper.HasRequiredPermissions())
            {
                Log.Warning("This application requires elevated permissions.");
                Console.WriteLine("⚠️  Warning: This application requires elevated permissions.");
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.WriteLine("   Please run with sudo: sudo dotnet run");
                }
                else
                {
                    Console.WriteLine("   Please run as Administrator on Windows.");
                }
                Console.WriteLine("\nPress any key to continue anyway...");
                Console.ReadKey();
                Console.WriteLine();
            }

            try
            {
                Log.Information("Initializing packet capture");
                PacketCapture capture = new();

                // List available interfaces
                List<DeviceInfo> devices = capture.GetDevices();
                if (devices.Count == 0)
                {
                    Log.Error("No network devices found");
                    Console.WriteLine("❌ No network devices found!");
                    Console.WriteLine("\nOn Windows: Install Npcap from https://npcap.com/");
                    Console.WriteLine("On Linux: Install libpcap-dev (sudo apt install libpcap-dev)");
                    return;
                }

                // Display available interfaces
                Console.WriteLine("Available Network Interfaces:");
                Console.WriteLine("─────────────────────────────────────────");
                for (int i = 0; i < devices.Count; i++)
                {
                    Console.WriteLine($"[{i}] {devices[i].Name}");
                    Console.WriteLine($"    Description: {devices[i].Description}");
                    Console.WriteLine($"    Addresses: {string.Join(", ", devices[i].Addresses)}");
                    Console.WriteLine();
                }

                // Select interface
                int selectedIndex;
                while (true)
                {
                    Console.Write($"Select interface (0-{devices.Count - 1}): ");
                    if (int.TryParse(Console.ReadLine(), out selectedIndex) &&
                        selectedIndex >= 0 && selectedIndex < devices.Count)
                    {
                        break;
                    }
                    Console.WriteLine("Invalid selection. Please try again.");
                }

                // Get capture options
                Console.Write("\nEnter packet count limit (0 for unlimited): ");
                if (!int.TryParse(Console.ReadLine(), out int packetLimit))
                {
                    packetLimit = 0;
                }

                Console.Write("Save to PCAP file? (y/n): ");
                bool saveToPcap = Console.ReadKey().KeyChar.ToString().ToLower() == "y";
                Console.WriteLine();

                string pcapFilePath = null;
                if (saveToPcap)
                {
                    pcapFilePath = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.pcap";
                    Console.WriteLine($"Packets will be saved to: {pcapFilePath}");
                }

                Console.WriteLine("\n╔══════════════════════════════════════════╗");
                Console.WriteLine("║  Starting Packet Capture...              ║");
                Console.WriteLine("║  Press Ctrl+C to stop                    ║");
                Console.WriteLine("╚══════════════════════════════════════════╝\n");

                // Set up cancellation
                CancellationTokenSource cts = new();
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("\n\n🛑 Stopping capture...");
                };

                // Start capture
                Log.Information("Starting capture on interface {InterfaceIndex} with limit {PacketLimit} and pcap {PcapPath}", selectedIndex, packetLimit, pcapFilePath ?? "(none)");
                capture.StartCapture(selectedIndex, packetLimit, pcapFilePath, cts.Token);

                Log.Information("Capture completed successfully");
                Console.WriteLine("\n✅ Capture completed successfully!");
                if (saveToPcap && pcapFilePath != null)
                {
                    Log.Information("Packets saved to {PcapPath}", pcapFilePath);
                    Console.WriteLine($"📁 Packets saved to: {pcapFilePath}");
                    Console.WriteLine("   You can analyze this file with Wireshark");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled error: {Message}", ex.Message);
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"Details: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    Log.Error(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            Log.CloseAndFlush();
        }
    }

    internal static class PermissionHelper
    {
        public static bool HasRequiredPermissions()
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Check if running as root
                    return Environment.UserName == "root";
                }
                else
                {
#if WINDOWS
                    // On Windows, check if running as administrator
                    using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
#else
                    return true;
#endif
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
