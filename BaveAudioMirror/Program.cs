﻿using NAudio.Wave;
using NAudio.CoreAudioApi;


class AudioMirror
{
    private static WasapiLoopbackCapture capture;
    private static WasapiOut waveOut;
    private static BufferedWaveProvider buffer;
    private static float currentVolume = 0f;
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        Console.WriteLine("===============================");
        Console.WriteLine("    Bave Audio Mirror Tool");
        Console.WriteLine("===============================");

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
            waveOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, true, 1);

            // Create buffer for audio data
            buffer = new BufferedWaveProvider(capture.WaveFormat);
            //buffer = new BufferedWaveProvider(targetDevice.AudioClient.MixFormat);

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

            // If users click arrow up or down, adjust volume
            ConsoleKeyInfo keyInfo;
            while (isRunning && (keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Escape)
            {
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    currentVolume = Math.Min(currentVolume + 0.1f, 1.0f);
                    waveOut.Volume = currentVolume;
                    Console.WriteLine($"Volume increased to: {currentVolume * 100}%");
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    currentVolume = Math.Max(currentVolume - 0.1f, 0.0f);
                    waveOut.Volume = currentVolume;
                    Console.WriteLine($"Volume decreased to: {currentVolume * 100}%");
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