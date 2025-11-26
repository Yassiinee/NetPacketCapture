using SharpPcap;
using SharpPcap.LibPcap;
using Serilog;

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
                Log.Information("PCAP writer initialized: {FilePath}", filePath);
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
                Log.Debug("WritePacket called with null packet - skipping");
                return;
            }

            try
            {
                _writer.Write(packet);
                Log.Verbose("Wrote packet to PCAP: Length={Length} Time={Time}", packet.Data?.Length ?? 0, packet.Timeval.Date);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to write packet to PCAP file: {Message}", ex.Message);
            }
        }

        public void Close()
        {
            Log.Debug("Close called on PcapWriter");
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _writer?.Close();
                    Log.Information("PCAP writer closed.");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Exception while closing PCAP writer: {Message}", ex.Message);
                }

                _disposed = true;
            }
        }
    }
}
