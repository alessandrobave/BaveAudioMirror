using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BaveAudioMirror
{
    internal static class udpReceiver
    {
        private static int sourcePort = 12345; // Example port number
        private static UdpClient udpClient;
        static IPEndPoint ipLocalEndPoint;
        static int receivedPacketCount = 0;

        private static WasapiOut waveOut;
        static BufferedWaveProvider buffer;

        public static void StartReceiving(int _deviceId)
        {
            // Open UDP socket
            udpClient = new UdpClient(sourcePort);

            Console.WriteLine($"UDP receiver started on port {sourcePort}");

            IPAddress ipAddress = Dns.Resolve(Dns.GetHostName()).AddressList[0];
            ipLocalEndPoint = new IPEndPoint(ipAddress, sourcePort);

            setupOutputDevice(_deviceId);

            // Start listening for incoming packets
            //Task.Run(() => ListenForPackets());

            ListenForPackets();
        }
        static void ListenForPackets()
        {
            while (true)
            {
                byte[] result = udpClient.Receive(ref ipLocalEndPoint);
                ProcessReceivedPacket(result);
            }
        }
        private static void ProcessReceivedPacket(byte[] packetData)
        {
            // Handle the received audio data here
            buffer.AddSamples(packetData, 0, packetData.Length);
            receivedPacketCount++;
            Console.WriteLine($"Received n° {receivedPacketCount} packets of size {packetData.Length} on port:{sourcePort} BuffDuration: {buffer.BufferDuration} Buffered: {buffer.BufferedDuration}");
            Console.SetCursorPosition(0, Console.CursorTop - 1);

        }



        static void setupOutputDevice(int deviceId)
        {
            // List available output devices
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            // Here you would set up the output device using the deviceId
            // This is a placeholder for actual audio output setup code
            Console.WriteLine($"Setting up output device with ID: {deviceId}");
            MMDevice targetDevice = devices[deviceId];

            Console.WriteLine($"Selected device: {targetDevice.FriendlyName}");
            // output targetDevice rate, bits, channels
            Console.WriteLine($"Device Rate: {targetDevice.AudioClient.MixFormat.SampleRate} Hz");
            Console.WriteLine($"Device Bits: {targetDevice.AudioClient.MixFormat.BitsPerSample} bits");
            Console.WriteLine($"Device Channels: {targetDevice.AudioClient.MixFormat.Channels} channels");
            Console.WriteLine($"Device ID: {targetDevice}");

            WaveFormat waveFormat = new WaveFormat(48000, 32, 2); // Example format
            Console.WriteLine($"Using WaveFormat: {waveFormat.SampleRate} Hz, {waveFormat.BitsPerSample} bits, {waveFormat.Channels} channels, Encoder {waveFormat.Encoding}");

            waveOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, true, 1);
            
            buffer = new BufferedWaveProvider(waveFormat);
            buffer.DiscardOnBufferOverflow = true;
            buffer.BufferDuration = TimeSpan.FromSeconds(0.06);

            waveOut.Init(buffer);

            waveOut.Play();

            Console.WriteLine($"Mirroring audio to: {targetDevice.FriendlyName}");

        }
    }
}
