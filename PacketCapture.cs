using Serilog;
using SharpPcap;
using SharpPcap.LibPcap;

namespace NetPacketCapture
{
    public class DeviceInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Addresses { get; set; }
    }

    public class PacketCapture
    {
        private readonly PacketDisplay _display;
        private PcapWriter _pcapWriter;
        private int _packetCount;
        private readonly object _lockObj = new();

        public PacketCapture()
        {
            _display = new PacketDisplay();
        }

        public List<DeviceInfo> GetDevices()
        {
            List<DeviceInfo> devices = new();
            CaptureDeviceList captureDevices = CaptureDeviceList.Instance;

            foreach (ILiveDevice? device in captureDevices)
            {
                DeviceInfo info = new()
                {
                    Name = device.Name,
                    Description = device.Description ?? "No description",
                    Addresses = new List<string>()
                };

                if (device is LibPcapLiveDevice pcapDevice)
                {
                    foreach (PcapAddress? addr in pcapDevice.Addresses)
                    {
                        if (addr.Addr != null)
                        {
                            info.Addresses.Add(addr.Addr.ToString());
                        }
                    }
                }

                if (!info.Addresses.Any())
                {
                    info.Addresses.Add("No IP addresses");
                }

                devices.Add(info);
            }

            return devices;
        }

        public void StartCapture(int deviceIndex, int packetLimit, string pcapFilePath, CancellationToken cancellationToken)
        {
            CaptureDeviceList devices = CaptureDeviceList.Instance;
            if (deviceIndex < 0 || deviceIndex >= devices.Count)
            {
                throw new ArgumentException("Invalid device index");
            }

            ILiveDevice device = devices[deviceIndex];

            // Initialize PCAP writer if needed
            if (!string.IsNullOrEmpty(pcapFilePath))
            {
                _pcapWriter = new PcapWriter(pcapFilePath);
            }

            _packetCount = 0;

            // Set up packet arrival handler
            device.OnPacketArrival += (sender, capture) =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        device.StopCapture();
                        return;
                    }

                    lock (_lockObj)
                    {
                        _packetCount++;

                        // Display packet
                        _display.DisplayPacket(_packetCount, capture.GetPacket());

                        // Save to PCAP if enabled
                        _pcapWriter?.WritePacket(capture.GetPacket());

                        // Check packet limit
                        if (packetLimit > 0 && _packetCount >= packetLimit)
                        {
                            device.StopCapture();
                            Log.Information("Reached packet limit {PacketLimit}", packetLimit);
                            Console.WriteLine($"\n✅ Reached packet limit ({packetLimit} packets)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error processing packet: {Message}", ex.Message);
                    Console.WriteLine($"\n⚠️  Error processing packet: {ex.Message}");
                }
            };

            try
            {
                // Open device for capture
                device.Open(DeviceModes.Promiscuous, 1000);

                Log.Information("Listening on {Description} ({Name}) in Promiscuous mode", device.Description, device.Name);
                Console.WriteLine($"📡 Listening on: {device.Description}");
                Console.WriteLine($"📍 Device: {device.Name}");
                Console.WriteLine($"🔓 Mode: Promiscuous (capturing all packets)\n");
                Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

                // Start capture
                device.StartCapture();

                // Wait for cancellation or completion
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(100);

                    lock (_lockObj)
                    {
                        if (packetLimit > 0 && _packetCount >= packetLimit)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                // Clean up
                if (device.Started)
                {
                    device.StopCapture();
                }
                device.Close();
                _pcapWriter?.Close();

                Log.Information("Total packets captured: {Count}", _packetCount);
                Console.WriteLine($"\n📊 Total packets captured: {_packetCount}");
            }
        }
    }
}
