using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;

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
        private readonly object _lockObj = new object();

        public PacketCapture()
        {
            _display = new PacketDisplay();
        }

        public List<DeviceInfo> GetDevices()
        {
            var devices = new List<DeviceInfo>();
            var captureDevices = CaptureDeviceList.Instance;

            foreach (var device in captureDevices)
            {
                var info = new DeviceInfo
                {
                    Name = device.Name,
                    Description = device.Description ?? "No description",
                    Addresses = new List<string>()
                };

                if (device is LibPcapLiveDevice pcapDevice)
                {
                    foreach (var addr in pcapDevice.Addresses)
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
            var devices = CaptureDeviceList.Instance;
            if (deviceIndex < 0 || deviceIndex >= devices.Count)
            {
                throw new ArgumentException("Invalid device index");
            }

            var device = devices[deviceIndex];
            
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
                            Console.WriteLine($"\n✅ Reached packet limit ({packetLimit} packets)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n⚠️  Error processing packet: {ex.Message}");
                }
            };

            try
            {
                // Open device for capture
                device.Open(DeviceModes.Promiscuous, 1000);

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

                Console.WriteLine($"\n📊 Total packets captured: {_packetCount}");
            }
        }
    }
}