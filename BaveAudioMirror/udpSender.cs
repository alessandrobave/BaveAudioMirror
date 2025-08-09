using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BaveAudioMirror
{
    internal static class udpSender
    {
        private static WasapiLoopbackCapture capture;
        private static string destIP;
        private static BufferedWaveProvider buffer;

        private static UdpClient udpClient;
        private static int destPort = 12345; // Example port number

        static DateTime lastSentPacketTimestamp;

        static int packetSentCount = 0;

        public static void StartSending(string _destIP,WasapiLoopbackCapture _capture)
        {
            destIP = _destIP;
            capture = _capture;

            // Open UDP socket
            udpClient = new UdpClient();
            udpClient.Connect(destIP, destPort); // Example port number

            // Code to start sending UDP packets
            Console.WriteLine($"UDP sender started to IP {destIP} to port {destPort}");

            WaveFormat captureWaveformat = new WaveFormat(
                48000, // Sample rate
                32,    // Bits per sample
                2     // Channels
            );

            // Setup loopback capture (captures system audio)
            capture = new WasapiLoopbackCapture();
            capture.WaveFormat = captureWaveformat; // Set the desired format

            // Handle captured audio data
            capture.DataAvailable += (sender, e) =>
            {
                // Convert captured audio data to PCM format


                udpClient.Send(e.Buffer, e.BytesRecorded);
                printStreamer(e.BytesRecorded);
            };

            capture.StartRecording();
            Console.WriteLine("");
            lastSentPacketTimestamp = DateTime.Now;

            Console.WriteLine($"Audio settings: " +
                $"CH:{capture.WaveFormat.Channels} " +
                $"Rate: {capture.WaveFormat.SampleRate} " +
                $"Bits: {capture.WaveFormat.BitsPerSample} " +
                $"Encoder: {capture.WaveFormat.Encoding}");

            while (true)
            {
                // Keep the application running while capturing audio
                System.Threading.Thread.Sleep(1000);
            }

        }

        public static void printStreamer(int bytesLen)
        {
            packetSentCount++;
            DateTime now = DateTime.Now;
            TimeSpan diff = now - lastSentPacketTimestamp;
            double bandwidth = (double)bytesLen * 8.0 / diff.TotalSeconds; // bits per second

            Console.WriteLine($"Packets sent: {packetSentCount}, bandwidth: {bandwidth / 1000 / 1000:F2}mbps");
            Console.SetCursorPosition(0, Console.CursorTop - 1);

            lastSentPacketTimestamp = DateTime.Now;
        }

    }
}
