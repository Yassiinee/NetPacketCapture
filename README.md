# NetPacketCapture

A lightweight, cross-platform packet sniffer built with .NET 10 that captures Layer 2 (Ethernet) packets with detailed hex and ASCII output. Supports packet capture on any network interface and saves packets to PCAP format for analysis.

## Features

- ✅ **Cross-Platform**: Works on Windows and Linux
- 🔍 **Layer 2 Capture**: Captures raw Ethernet frames
- 📊 **Protocol Analysis**: Parses Ethernet, IP, TCP, UDP, ARP, ICMP
- 🎨 **Colorized Output**: Beautiful console output with syntax highlighting
- 📝 **Hex/ASCII Dump**: Detailed packet inspection with hex and ASCII views
- 💾 **PCAP Export**: Save captured packets in standard PCAP format
- 🎯 **Flexible Filtering**: Configurable packet count limits
- 🔓 **Promiscuous Mode**: Capture all network traffic on the interface

## Prerequisites

### Windows
- **Npcap**: Download and install from [https://npcap.com/](https://npcap.com/)
  - During installation, check "Install Npcap in WinPcap API-compatible Mode"
- **Administrator Rights**: Run the application as Administrator

### Linux
- **libpcap**: Install development package
  ```bash
  # Debian/Ubuntu
  sudo apt-get install libpcap-dev
  
  # RHEL/CentOS/Fedora
  sudo yum install libpcap-devel
  
  # Arch Linux
  sudo pacman -S libpcap
  ```
- **Root/Sudo**: Run with sudo for raw packet capture
  ```bash
  sudo dotnet run
  ```

## Installation

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd NetPacketCapture
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the application**
   
   **Windows (as Administrator):**
   ```bash
   dotnet run
   ```
   
   **Linux (with sudo):**
   ```bash
   sudo dotnet run
   ```

## Usage

1. **Select Network Interface**: Choose from the list of available interfaces
2. **Set Packet Limit**: Enter the number of packets to capture (0 for unlimited)
3. **Enable PCAP Export**: Choose whether to save packets to a PCAP file
4. **Capture Packets**: Watch real-time packet capture with detailed analysis
5. **Stop Capture**: Press `Ctrl+C` to stop capturing

### Example Output

```
╔═══ Packet #1 ═══════════════════════════════════════════════
📅 Timestamp: 2024-11-25 14:32:15.123
📦 Length: 74 bytes
🔗 Layer 2 - Ethernet II
   Source MAC: 00:1A:2B:3C:4D:5E
   Dest MAC:   FF:FF:FF:FF:FF:FF
   Type:       IPv4 (0x0800)
🌐 Layer 3 - IPv4
   Source IP:  192.168.1.100
   Dest IP:    192.168.1.1
   Protocol:   Tcp
   TTL:        64
🔌 Layer 4 - TCP
   Source Port:      443
   Dest Port:        52345
   Flags:            SYN ACK

📋 Hex Dump:
   0000: FF FF FF FF FF FF 00 1A 2B 3C 4D 5E 08 00 45 00  ........+<M^..E.
   0010: 00 3C 1C 46 40 00 40 06 B1 E6 C0 A8 01 64 C0 A8  .<.F@.@......d..
╚═══════════════════════════════════════════════════════════════
```

## Supported Protocols

- **Layer 2**: Ethernet II
- **Layer 3**: IPv4, IPv6, ARP
- **Layer 4**: TCP, UDP, ICMP, ICMPv6

## PCAP File Analysis

Captured packets are saved in standard PCAP format. You can analyze them with:

- **Wireshark**: `wireshark capture_20241125_143215.pcap`
- **tcpdump**: `tcpdump -r capture_20241125_143215.pcap`
- **tshark**: `tshark -r capture_20241125_143215.pcap`

## Project Structure

```
NetPacketCapture/
├── Program.cs              # Main entry point and CLI interface
├── PacketCapture.cs        # Core packet capture logic
├── PacketDisplay.cs        # Hex/ASCII formatting and display
├── PcapWriter.cs          # PCAP file writer
├── NetPacketCapture.csproj # Project configuration
└── README.md              # This file
```

## Dependencies

- **SharpPcap 6.3.0**: Cross-platform packet capture library
- **PacketDotNet 1.4.7**: Packet parsing and protocol analysis

## Troubleshooting

### "No network devices found"
- **Windows**: Install Npcap from https://npcap.com/
- **Linux**: Install libpcap-dev package

### "Access Denied" or Permission Errors
- **Windows**: Run as Administrator
- **Linux**: Run with sudo (`sudo dotnet run`)

### PCAP file not created
- Check write permissions in the current directory
- Ensure sufficient disk space

## Security Considerations

⚠️ **Warning**: This tool captures network traffic which may contain sensitive information. 

- Only use on networks you own or have explicit permission to monitor
- Be aware of local laws regarding network monitoring
- Captured PCAP files may contain passwords and sensitive data
- Never share PCAP files without reviewing their contents

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the MIT License.

## Acknowledgments

- Built with [SharpPcap](https://github.com/chmorgan/sharppcap)
- Protocol parsing with [PacketDotNet](https://github.com/chmorgan/packetnet)

## Author
Yassine Zakhama
Created as part of .NET 10 development learning and testing.