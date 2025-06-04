# Bave Audio Mirror Tool 🎧

A simple portable windows C# console application to mirror your system's audio output to a selected audio device. This can be useful for routing audio to virtual audio cables, other sound cards, or any connected audio output.

---

## 📜 Description

The **Bave Audio Mirror Tool** captures the system's audio output (what you hear) using WASAPI Loopback and then plays it back to a user-selected audio output device. It lists all available active output devices and allows the user to choose where the system audio should be mirrored.

---

## ✨ Features

* **Lists available audio output devices:** Shows a numbered list of all active sound output devices.
* **User-selectable target device:** Allows you to choose which device to mirror the audio to.
* **Real-time audio mirroring:** Captures and plays back audio with low latency.
* **Simple console interface:** Easy to use with straightforward instructions.

---

## ⚙️ How It Works

1.  **Device Enumeration:** On startup, the application uses `MMDeviceEnumerator` from NAudio to list all active audio rendering (output) devices.
2.  **User Selection:** The user is prompted to select one of the listed devices by entering its corresponding number.
3.  **Loopback Capture:** `WasapiLoopbackCapture` is initialized to capture the system's audio mix. This is essentially "what you hear" from your default playback device.
4.  **Output Playback:** `WasapiOut` is initialized to play audio to the user-selected target device.
5.  **Buffering:** A `BufferedWaveProvider` is used to queue the audio data captured from the loopback device before it's sent to the output device. This helps to ensure smooth playback.
6.  **Data Flow:**
    * The `DataAvailable` event of `WasapiLoopbackCapture` fires whenever new audio data is captured.
    * This data ( `e.Buffer` ) is added to the `BufferedWaveProvider`.
    * `WasapiOut` reads from the `BufferedWaveProvider` and plays the audio on the chosen target device.
7.  **Control:** The mirroring process starts immediately after device selection and continues until the user presses any key.
8.  **Cleanup:** Upon stopping, all resources (`WasapiLoopbackCapture` and `WasapiOut` instances) are properly disposed of.

---

## 🚀 Getting Started

### Prerequisites

* **.NET SDK:** Ensure you have the .NET SDK installed to build and run the application. (The specific version depends on your project file, but .NET Core 3.1 or .NET 5/6/7/8 should generally work with NAudio).
* **NAudio Library:** This project relies on the NAudio NuGet package.

### Installation & Running

1.  **Clone the repository or download the source code.**
    ```bash
    git clone <repository-url>
    cd <repository-directory>
    ```
2.  **Restore NuGet packages:** If you have the project file (`.csproj`), navigate to the project directory in your terminal and run:
    ```bash
    dotnet restore
    ```
    This will download and install NAudio if it's listed in the project file. If you only have the `.cs` file, you'll need to create a new console project and add NAudio:
    ```bash
    dotnet new console -n AudioMirrorApp
    cd AudioMirrorApp
    dotnet add package NAudio
    # Then replace the generated Program.cs content with the provided code.
    ```
3.  **Build the application:**
    ```bash
    dotnet build
    ```
4.  **Run the application:**
    ```bash
    dotnet run
    ```
    Alternatively, you can run the compiled executable directly from the `bin/Debug/<target-framework>` folder.

### Usage

1.  When you run the application, you'll see the "Bave Audio Mirror Tool" welcome message.
2.  A list of available audio output devices will be displayed, each with a number.
3.  Enter the number corresponding to the device you want to mirror the audio **to**.
4.  Press Enter. The audio mirroring will start.
5.  To stop mirroring, press any key in the console window.

---

## 📦 Dependencies

* **NAudio:** A comprehensive open-source audio library for .NET. (Specifically `NAudio.Wave` for general waveform manipulation and `NAudio.CoreAudioApi` for WASAPI access).

    * [NAudio on NuGet](https://www.nuget.org/packages/NAudio/)
    * [NAudio on GitHub](https://github.com/naudio/NAudio)

---

## ⚠️ Important Notes

* **Administrator Privileges:** Depending on your system configuration and the audio drivers, you *might* need to run the application with administrator privileges for it to correctly access and control audio devices, though it's generally not required for loopback.
* **Audio Feedback Loops:** Be careful not to create a feedback loop by selecting your primary listening device as the target if the loopback is also capturing from it without proper management (e.g., if you have "Listen to this device" enabled in Windows sound settings for your microphone and you mirror to your speakers). This tool is primarily designed to send system audio to a *different* output.
* **Error Handling:** Basic error handling is implemented, but specific audio driver issues might lead to unexpected behavior.
* **Latency:** While WASAPI is low-latency, there will always be some inherent latency in the capture and playback process.

---


Enjoy mirroring your audio! 🔊
