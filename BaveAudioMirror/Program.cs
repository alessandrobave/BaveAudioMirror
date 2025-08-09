using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using BaveAudioMirror;

class AudioMirror
{
    private static WasapiLoopbackCapture capture;
    private static WasapiOut waveOut;
    private static BufferedWaveProvider buffer;
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        Console.WriteLine("===============================");
        Console.WriteLine("    Bave Audio Mirror Tool     ");
        Console.WriteLine("===============================");

        try
        {
            // List available output devices
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            audioMirrorSettings currentSettings = new audioMirrorSettings(); // Initialize settings

            //PARSING ARGUMENTS
            string? deviceName = null;
            float? volumeArg = null;
            bool enableLowpass = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--device" && i + 1 < args.Length)
                {
                    deviceName = args[i + 1];
                    i++;
                }
                else if (args[i] == "--volume" && i + 1 < args.Length)
                {
                    if (float.TryParse(args[i + 1], out float v))
                    {
                        volumeArg = Math.Clamp(v, 0f, 1f);
                        Console.WriteLine($"Volume set to: {volumeArg.Value * 100:F0}%");
                    }
                    i++;
                }
                else if (args[i] == "--lowpass")
                {
                    enableLowpass = true;
                    Console.WriteLine("Low-pass filter enabled.");
                }
            }

            if (volumeArg.HasValue)
                currentSettings.Volume = volumeArg.Value;
            if (enableLowpass)
                currentSettings.IsFilterEnabled = true;

            BiQuadFilter biquadFilter = BiQuadFilter.LowPassFilter(44100, currentSettings.StartFilterFrequency, currentSettings.FilterQ);



            // Display available devices
            Console.WriteLine("\nAvailable output devices:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i}: {devices[i].FriendlyName}");
            }
            Console.WriteLine($"IP: UDP Sender");
            Console.WriteLine($"CL: UDP Client");

            int deviceIndex = -1;
            if (!string.IsNullOrEmpty(deviceName))
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    if (devices[i].FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        deviceIndex = i;
                        break;
                    }
                }
                if (deviceIndex == -1)
                {
                    Console.WriteLine($"Device '{deviceName}' not found.");
                    return;
                }
                Console.WriteLine($"\nSelected device via --device: {devices[deviceIndex].FriendlyName}");
            }
            else
            {
                // Get user choice
                Console.Write("\nSelect target device (number): ");
                string selection = Console.ReadLine();

                if (selection.ToUpper().StartsWith("IP"))
                {
                    // Handle UDP Streamer
                    Console.WriteLine("UDP Streamer selected, parsing IP");
                    string ipAddress = selection.Substring(3).Trim();

                    // check if IP is valid
                    if (!System.Net.IPAddress.TryParse(ipAddress, out _))
                    {
                        Console.WriteLine("Invalid IP address format.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"UDP Streamer selected with IP: {ipAddress}");
                        // Start UDP sender
                        udpSender.StartSending(ipAddress, capture);

                        return;
                    }

                    return;
                }

                if (selection.ToUpper().StartsWith("CL"))
                {
                    // Handle UDP Client
                    Console.WriteLine("UDP Client selected");

                    string deviceID = selection.Substring(3).Trim();

                    // check if ID is valid
                    if (!int.TryParse(deviceID, out deviceIndex) || deviceIndex < 0 || deviceIndex >= devices.Count)
                    {
                        Console.WriteLine("Invalid device selection.");
                        return;
                    }

                    udpReceiver.StartReceiving(deviceIndex);
                    return;

                }

                if (!int.TryParse(selection, out deviceIndex) ||
                    deviceIndex < 0 || deviceIndex >= devices.Count)
                {
                    Console.WriteLine("Invalid device selection.");
                    return;
                }
            }

            var targetDevice = devices[deviceIndex];
            Console.WriteLine($"Mirroring audio to: {targetDevice.FriendlyName}");
            Console.WriteLine($"Audio settings: " +
                $"CH:{targetDevice.AudioClient.MixFormat.Channels} " +
                $"Rate: {targetDevice.AudioClient.MixFormat.SampleRate} " +
                $"Bits: {targetDevice.AudioClient.MixFormat.BitsPerSample} " +
                $"Encoder: {targetDevice.AudioClient.MixFormat.Encoding}");

            Console.WriteLine("Press any key to stop...\n");

            // Setup loopback capture (captures system audio)
            capture = new WasapiLoopbackCapture();

            // Setup output to selected device
            waveOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, true, 1);

            // Create buffer for audio data
            buffer = new BufferedWaveProvider(capture.WaveFormat);

            // Handle captured audio data
            capture.DataAvailable += (sender, e) =>
            {
                buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            // Start capture and playback
            waveOut.Init(buffer);
            capture.StartRecording();
            waveOut.Volume = currentSettings.Volume; // Set initial volume
            waveOut.Play();

            // Wait for user input to stop
            Console.ReadKey();

            // If users click arrow up or down, adjust volume
            ConsoleKeyInfo keyInfo;
            int volumeLinePosition = Console.CursorTop;
            Console.WriteLine("Volume: 0%");  // Initial display
            int filterLinePosition = volumeLinePosition + 1;

            while (isRunning && (keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Escape)
            {
                // Change volume
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    currentSettings.Volume = Math.Min(currentSettings.Volume + 0.1f, 1.0f);
                    waveOut.Volume = currentSettings.Volume;
                    Console.SetCursorPosition(0, volumeLinePosition);
                    Console.Write($"Volume: {currentSettings.Volume * 100:F0}%        ");  // Extra spaces to clear previous text
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    currentSettings.Volume = Math.Max(currentSettings.Volume - 0.1f, 0.0f);
                    waveOut.Volume = currentSettings.Volume;
                    Console.SetCursorPosition(0, volumeLinePosition);
                    Console.Write($"Volume: {currentSettings.Volume * 100:F0}%        ");  // Extra spaces to clear previous text
                }

                // Change frequency
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    // Increase filter frequency by 10 Hz
                    currentSettings.StartFilterFrequency += 10;
                    biquadFilter.SetLowPassFilter(44100, currentSettings.StartFilterFrequency, currentSettings.FilterQ);
                    Console.SetCursorPosition(0, filterLinePosition);
                    Console.Write($"Filter: {currentSettings.StartFilterFrequency}hz        ");
                }
                else if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    // Decrease filter frequency by 10 Hz
                    currentSettings.StartFilterFrequency = Math.Max(10, currentSettings.StartFilterFrequency - 10); // Prevent negative frequency
                    biquadFilter.SetLowPassFilter(44100, currentSettings.StartFilterFrequency, currentSettings.FilterQ);
                    Console.SetCursorPosition(0, filterLinePosition);
                    Console.Write($"Filter: {currentSettings.StartFilterFrequency}hz        ");
                }

                // Change q factor
                else if (keyInfo.Key == ConsoleKey.PageDown)
                {
                    currentSettings.FilterQ = Math.Max(0.01f, currentSettings.FilterQ - 0.05f); // Prevent negative Q factor
                    biquadFilter.SetLowPassFilter(44100, currentSettings.StartFilterFrequency, currentSettings.FilterQ);
                    Console.SetCursorPosition(0, filterLinePosition);
                    Console.Write($"Filter Q: {currentSettings.FilterQ:F2}             ");
                }
                else if (keyInfo.Key == ConsoleKey.PageUp)
                {
                    currentSettings.FilterQ += 0.05f; // Increase Q factor
                    biquadFilter.SetLowPassFilter(44100, currentSettings.StartFilterFrequency, currentSettings.FilterQ);
                    Console.SetCursorPosition(0, filterLinePosition);
                    Console.Write($"Filter Q: {currentSettings.FilterQ:F2}               ");
                }

                // if user press "l" disable filter
                else if (keyInfo.Key == ConsoleKey.L)
                {
                    if (!currentSettings.IsFilterEnabled)
                    {
                        currentSettings.IsFilterEnabled = true;

                        // Cleanup
                        capture?.StopRecording();
                        waveOut?.Stop();
                        capture?.Dispose();
                        waveOut?.Dispose();

                        // Setup loopback capture (captures system audio)
                        capture = new WasapiLoopbackCapture();

                        // Setup output to selected device
                        waveOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, true, 5);

                        // Create buffer for audio data
                        buffer = new BufferedWaveProvider(capture.WaveFormat);

                        // Handle captured audio data
                        capture.DataAvailable += (sender, e) =>
                        {
                            buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                        };

                        // Apply BiQuad filter to the audio
                        ISampleProvider sampleProvider = buffer.ToSampleProvider();
                        BiQuadFilterProvider filteredProvider = new BiQuadFilterProvider(sampleProvider, biquadFilter);

                        // Start capture and playback
                        waveOut.Init(filteredProvider);
                        capture.StartRecording();
                        waveOut.Play();

                        Console.SetCursorPosition(0, filterLinePosition);
                        Console.Write("Filter: LowPass Filter Enabled     ");
                    }

                    else
                    {
                        currentSettings.IsFilterEnabled = false;
                        // Cleanup
                        capture?.StopRecording();
                        waveOut?.Stop();
                        capture?.Dispose();
                        waveOut?.Dispose();
                        // Setup loopback capture (captures system audio)
                        capture = new WasapiLoopbackCapture();
                        // Setup output to selected device
                        waveOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, true, 5);
                        // Create buffer for audio data
                        buffer = new BufferedWaveProvider(capture.WaveFormat);
                        // Handle captured audio data
                        capture.DataAvailable += (sender, e) =>
                        {
                            buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                        };
                        // Start capture and playback
                        waveOut.Init(buffer);
                        capture.StartRecording();
                        waveOut.Play();
                        Console.SetCursorPosition(0, filterLinePosition);
                        Console.Write("Filter: LowPass Filter Disabled     ");

                    }


                }


                else if (keyInfo.Key == ConsoleKey.Q || keyInfo.Key == ConsoleKey.E)
                {
                    break; // Exit on Q or E key
                }

            }

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
}

// Helper class to create a BiQuadFilter provider
public class BiQuadFilterProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly BiQuadFilter filter;

    public BiQuadFilterProvider(ISampleProvider source, BiQuadFilter filter)
    {
        this.source = source;
        this.filter = filter;
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);
        
        // Apply filter
        for (int i = 0; i < samplesRead; i++)
        {
            buffer[offset + i] = filter.Transform(buffer[offset + i]);
        }
        
        return samplesRead;
    }
}

public class audioMirrorSettings
{
    // Settings for the audio mirror tool can be added here
    public int StartFilterFrequency { get; set; } = 100; // Default filter frequency
    public float FilterQ { get; set; } = 0.15f; // Default filter Q factor
    public bool IsFilterEnabled { get; set; } = false; // Initial filter state
    public float Volume { get; set; } = 0.5f; // Default volume level

}




