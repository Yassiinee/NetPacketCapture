using System.Text;
using PacketDotNet;
using SharpPcap;

namespace NetPacketCapture
{
    public class PacketDisplay
    {
        public void DisplayPacket(int packetNumber, RawCapture rawPacket)
        {
            Packet packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"╔═══ Packet #{packetNumber} ═══════════════════════════════════════════════");
            Console.ResetColor();

            Console.WriteLine($"Timestamp: {rawPacket.Timeval.Date:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Length: {rawPacket.Data.Length} bytes");

            // Parse Ethernet layer
            if (packet is EthernetPacket ethernetPacket)
            {
                DisplayEthernetInfo(ethernetPacket);

                // Parse IP layer
                IPPacket ipPacket = packet.Extract<IPPacket>();
                if (ipPacket != null)
                {
                    DisplayIpInfo(ipPacket);

                    // Parse Transport layer
                    if (ipPacket.Protocol == ProtocolType.Tcp)
                    {
                        TcpPacket tcpPacket = packet.Extract<TcpPacket>();
                        if (tcpPacket != null) DisplayTcpInfo(tcpPacket);
                    }
                    else if (ipPacket.Protocol == ProtocolType.Udp)
                    {
                        UdpPacket udpPacket = packet.Extract<UdpPacket>();
                        if (udpPacket != null) DisplayUdpInfo(udpPacket);
                    }
                    else if (ipPacket.Protocol == ProtocolType.Icmp ||
                             ipPacket.Protocol == ProtocolType.IcmpV6)
                    {
                        Console.WriteLine($"🔔 ICMP Packet");
                    }
                }
                else if (ethernetPacket.Type == EthernetType.Arp)
                {
                    ArpPacket arpPacket = packet.Extract<ArpPacket>();
                    if (arpPacket != null) DisplayArpInfo(arpPacket);
                }
            }

            // Display hex dump
            Console.WriteLine("\n📋 Hex Dump:");
            DisplayHexDump(rawPacket.Data);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void DisplayEthernetInfo(EthernetPacket eth)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("🔗 Layer 2 - Ethernet II");
            Console.ResetColor();
            Console.WriteLine($"   Source MAC: {eth.SourceHardwareAddress}");
            Console.WriteLine($"   Dest MAC:   {eth.DestinationHardwareAddress}");
            Console.WriteLine($"   Type:       {eth.Type} (0x{(ushort)eth.Type:X4})");
        }

        private void DisplayIpInfo(IPPacket ip)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🌐 Layer 3 - {(ip.Version == IPVersion.IPv4 ? "IPv4" : "IPv6")}");
            Console.ResetColor();
            Console.WriteLine($"   Source IP:  {ip.SourceAddress}");
            Console.WriteLine($"   Dest IP:    {ip.DestinationAddress}");
            Console.WriteLine($"   Protocol:   {ip.Protocol}");
            Console.WriteLine($"   TTL:        {ip.TimeToLive}");
            Console.WriteLine($"   Length:     {ip.TotalLength} bytes");
        }

        private void DisplayTcpInfo(TcpPacket tcp)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("🔌 Layer 4 - TCP");
            Console.ResetColor();
            Console.WriteLine($"   Source Port:      {tcp.SourcePort}");
            Console.WriteLine($"   Dest Port:        {tcp.DestinationPort}");
            Console.WriteLine($"   Sequence:         {tcp.SequenceNumber}");
            Console.WriteLine($"   Acknowledgment:   {tcp.AcknowledgmentNumber}");
            Console.WriteLine($"   Flags:            {GetTcpFlags(tcp)}");
            Console.WriteLine($"   Window:           {tcp.WindowSize}");
            Console.WriteLine($"   Payload:          {tcp.PayloadData?.Length ?? 0} bytes");
        }

        private void DisplayUdpInfo(UdpPacket udp)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("🔌 Layer 4 - UDP");
            Console.ResetColor();
            Console.WriteLine($"Source Port:  {udp.SourcePort}");
            Console.WriteLine($"Dest Port:    {udp.DestinationPort}");
            Console.WriteLine($"Length:       {udp.Length} bytes");
            Console.WriteLine($"Payload: {udp.PayloadData?.Length ?? 0} bytes");
        }

        private void DisplayArpInfo(ArpPacket arp)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("📢 ARP Packet");
            Console.ResetColor();
            Console.WriteLine($"   Operation:       {arp.Operation}");
            Console.WriteLine($"   Sender MAC:      {arp.SenderHardwareAddress}");
            Console.WriteLine($"   Sender IP:       {arp.SenderProtocolAddress}");
            Console.WriteLine($"   Target MAC:      {arp.TargetHardwareAddress}");
            Console.WriteLine($"   Target IP:       {arp.TargetProtocolAddress}");
        }

        private string GetTcpFlags(TcpPacket tcp)
        {
            StringBuilder flags = new();
            if (tcp.Synchronize) flags.Append("SYN ");
            if (tcp.Acknowledgment) flags.Append("ACK ");
            if (tcp.Push) flags.Append("PSH ");
            if (tcp.Reset) flags.Append("RST ");
            if (tcp.Finished) flags.Append("FIN ");
            if (tcp.Urgent) flags.Append("URG ");
            return flags.Length > 0 ? flags.ToString().Trim() : "None";
        }

        private void DisplayHexDump(byte[] data)
        {
            const int bytesPerLine = 16;

            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                // Offset
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"   {i:X4}: ");
                Console.ResetColor();

                // Hex bytes
                int lineLength = Math.Min(bytesPerLine, data.Length - i);
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j < lineLength)
                    {
                        Console.Write($"{data[i + j]:X2} ");
                    }
                    else
                    {
                        Console.Write("   ");
                    }

                    if (j == 7) Console.Write(" "); // Extra space in the middle
                }

                // ASCII representation
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                for (int j = 0; j < lineLength; j++)
                {
                    byte b = data[i + j];
                    char c = (b >= 32 && b <= 126) ? (char)b : '.';
                    Console.Write(c);
                }
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }
}