using System;
using System.Linq;
using NAudio.Wave;
using NAudio.CoreAudioApi;

class AudioMirror
{
    private static WasapiLoopbackCapture capture;
    private static WaveOutEvent waveOut;
    private static BufferedWaveProvider buffer;

    static void Main(string[] args)
    {
        Console.WriteLine("Bave Audio Mirror Tool");
        Console.WriteLine("================");

        try
        {
            // List available output devices
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            Console.WriteLine("\nAvailable output devices:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i}: {devices[i].FriendlyName}");
            }

            // Get user choice
            Console.Write("\nSelect target device (number): ");
            if (!int.TryParse(Console.ReadLine(), out int deviceIndex) ||
                deviceIndex < 0 || deviceIndex >= devices.Count)
            {
                Console.WriteLine("Invalid device selection.");
                return;
            }

            var targetDevice = devices[deviceIndex];
            Console.WriteLine($"Mirroring audio to: {targetDevice.FriendlyName}");
            Console.WriteLine("Press any key to stop...\n");

            // Setup loopback capture (captures system audio)
            capture = new WasapiLoopbackCapture();

            // Setup output to selected device
            waveOut = new WaveOutEvent();
            waveOut.DeviceNumber = deviceIndex;

            // Create buffer for audio data
            buffer = new BufferedWaveProvider(capture.WaveFormat)
            {
                BufferLength = 1024 * 1024, // 1MB buffer
                DiscardOnBufferOverflow = true
            };

            // Handle captured audio data
            capture.DataAvailable += (sender, e) =>
            {
                buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            // Start capture and playback
            waveOut.Init(buffer);
            capture.StartRecording();
            waveOut.Play();

            // Wait for user input to stop
            Console.ReadKey();

            // Cleanup
            capture?.StopRecording();
            waveOut?.Stop();
            capture?.Dispose();
            waveOut?.Dispose();

            Console.WriteLine("\nAudio mirroring stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}