
namespace NetPacketCapture
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║   NetPacketCapture - Packet Sniffer      ║");
            Console.WriteLine("║   Layer 2 Ethernet Frame Capture         ║");
            Console.WriteLine("╚══════════════════════════════════════════╝\n");

            // Check if running with appropriate permissions
            if (!PermissionHelper.HasRequiredPermissions())
            {
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
                PacketCapture capture = new();

                // List available interfaces
                List<DeviceInfo> devices = capture.GetDevices();
                if (devices.Count == 0)
                {
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
                capture.StartCapture(selectedIndex, packetLimit, pcapFilePath, cts.Token);

                Console.WriteLine("\n✅ Capture completed successfully!");
                if (saveToPcap && pcapFilePath != null)
                {
                    Console.WriteLine($"📁 Packets saved to: {pcapFilePath}");
                    Console.WriteLine("   You can analyze this file with Wireshark");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"Details: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
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