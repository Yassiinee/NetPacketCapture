using SharpPcap;
using SharpPcap.LibPcap;

namespace NetPacketCapture
{
    public class PcapWriter : IDisposable
    {
        private readonly CaptureFileWriterDevice _writer;
        private bool _disposed;

        public PcapWriter(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            try
            {
                // Create PCAP writer
                _writer = new CaptureFileWriterDevice(filePath, System.IO.FileMode.Create);
                _writer.Open();

                Console.WriteLine($"✅ PCAP writer initialized: {filePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create PCAP writer: {ex.Message}", ex);
            }
        }

        public void WritePacket(RawCapture packet)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PcapWriter));
            }

            if (packet == null)
            {
                return;
            }

            try
            {
                _writer.Write(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Failed to write packet to PCAP file: {ex.Message}");
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _writer?.Close();
                _disposed = true;
            }
        }
    }
}