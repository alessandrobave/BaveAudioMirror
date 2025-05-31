using System;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;

class AudioMirror
{
    private static WasapiLoopbackCapture capture;
    private static WaveOutEvent waveOut;
    private static BufferedWaveProvider buffer;
    private static float currentVolume = 0f;
    private static bool isRunning = true;

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
                currentVolume = CalculateLevel(e.Buffer, e.BytesRecorded);
            };

            // Start capture and playback
            waveOut.Init(buffer);
            capture.StartRecording();
            waveOut.Play();

            // Start VU meter thread
            Thread vuMeterThread = new Thread(ShowVuMeter)
            {
                IsBackground = true
            };
            vuMeterThread.Start();

            // Wait for user input to stop
            Console.ReadKey();
            isRunning = false;

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

    private static float CalculateLevel(byte[] buffer, int bytesRecorded)
    {
        // Peak detection only for maximum responsiveness
        double peakValue = 0;
        
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            if (i + 1 < bytesRecorded)
            {
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                double normalizedSample = Math.Abs(sample / 32768.0);
                
                // Track peak only
                if (normalizedSample > peakValue)
                    peakValue = normalizedSample;
            }
        }
        
        // Minimum threshold to reduce background noise
        if (peakValue < 0.006) return 0;
        
        // Tuned curve for peak detection
        return (float)Math.Min(1.0, Math.Pow(peakValue * 4.5, 0.65));
    }

    private static void ShowVuMeter()
    {
        const int meterWidth = 50;
        float lastPeakValue = 0;
        int peakHoldTime = 0;
        const int peakHoldDuration = 8; // Even shorter hold time for peak display
        float smoothedVolume = 0;
        
        while (isRunning)
        {
            // Minimal smoothing for ultra-fast response
            smoothedVolume = smoothedVolume * 0.3f + currentVolume * 0.7f;
            
            // Calculate number of blocks to display
            int barSize = (int)(smoothedVolume * meterWidth);
            
            // Peak management with faster decay
            if (smoothedVolume >= lastPeakValue)
            {
                lastPeakValue = smoothedVolume;
                peakHoldTime = peakHoldDuration;
            }
            else if (peakHoldTime > 0)
            {
                peakHoldTime--;
            }
            else
            {
                lastPeakValue = Math.Max(0, lastPeakValue - 0.04f); // Even faster decay
            }
            
            int peakPosition = (int)(lastPeakValue * meterWidth);
            
            // Build VU meter bar with color gradient
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("PEAK: [");
            
            for (int i = 0; i < meterWidth; i++)
            {
                if (i < barSize)
                {
                    if (i < meterWidth * 0.6)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (i < meterWidth * 0.8)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.Write("█");
                }
                else if (i == peakPosition)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("▌");
                }
                else
                {
                    Console.Write(" ");
                }
            }
            
            Console.ResetColor();
            Console.Write($"] {smoothedVolume:F2}   ");
            
            Thread.Sleep(16); // Even faster update rate (~60fps)
        }
    }
}