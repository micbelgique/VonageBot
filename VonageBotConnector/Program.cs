using Fleck;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Dialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FleckWebsocketRecorder
{
    class Program
    {
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        private static IWebSocketConnection sock;
        private static bool locked = false;
        static void Main(string[] args)
        {
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.RestartAfterListenError = true;
            server.ListenerSocket.NoDelay = true;


            const string speechSubscriptionKey = ""; // Your subscription key
            const string region = ""; // Your subscription service region.

            var botConfig = BotFrameworkConfig.FromSubscription(speechSubscriptionKey, region);
            botConfig.Language = "fr-FR";
            

            var audioStream = new VoiceAudioStream();

            // Initialize with the format required by the Speech service
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
            // Configure speech SDK to work with the audio stream in right format.
            // Alternatively this can be a direct microphone input.
            var audioConfig = AudioConfig.FromStreamInput(audioStream, audioFormat);
            var connector = new DialogServiceConnector(botConfig, audioConfig);

            // Get credentials and region from client's message.
            server.Start(socket =>
                {
                    Console.WriteLine("started!");
                    sock = socket;
                    socket.OnOpen = () =>
                    {
                        Console.WriteLine("Open!");
                    };

                    socket.OnClose = () =>
                    {
                        Console.WriteLine("Close!");
                        connector.DisconnectAsync();
                    };
                    socket.OnMessage = message =>
                    {
                        connector.ListenOnceAsync();
                        Console.WriteLine(message);
                    };
                    socket.OnBinary = binary =>
                    {
                        if(!locked)
                            audioStream.Write(binary, 0, binary.Length);
                    };
                });

            Console.WriteLine("Open!");
            connector.ActivityReceived += (sender, activityReceivedEventArgs) =>
            {
                locked = true;
                Console.WriteLine(
                    $"Activity received, hasAudio={activityReceivedEventArgs.HasAudio} activity={activityReceivedEventArgs.Activity}");

                if (!activityReceivedEventArgs.HasAudio) return;
                byte[] pullBuffer;

                uint lastRead = 0;
                var numberByte = 0;
                var listByte = new List<byte[]>();
                var stopWatch = new Stopwatch();
                do
                {
                    pullBuffer = new byte[640];
                    lastRead = activityReceivedEventArgs.Audio.Read(pullBuffer);
                    numberByte += pullBuffer.Length;
                    listByte.Add(pullBuffer);
                }
                while (lastRead == pullBuffer.Length);
                stopWatch.Start();
                foreach (var byteArray in listByte)
                {
                    sock.Send(byteArray);
                    Console.WriteLine($"envois à Nexmo {stopWatch.ElapsedMilliseconds}");
                    Thread.Sleep(1);
                }

                // Get the elapsed time as a TimeSpan value.
                var wait = (((numberByte * 8) / 16000) / 16) * 1000 - stopWatch.ElapsedMilliseconds;
                Console.WriteLine(wait);
                Thread.Sleep((int)wait);
                stopWatch.Stop();
                locked = false;
                connector.ListenOnceAsync();
            };
            connector.Canceled += (sender, canceledEventArgs) =>
            {
                Console.WriteLine($"Canceled, reason={canceledEventArgs.Reason}");
                if (canceledEventArgs.Reason == CancellationReason.Error)
                {
                    Console.WriteLine(
                        $"Error: code={canceledEventArgs.ErrorCode}, details={canceledEventArgs.ErrorDetails}");
                }
            };
            connector.Recognizing += (sender, recognitionEventArgs) =>
            {
                Console.WriteLine($"Recognizing! in-progress text={recognitionEventArgs.Result.Text}");
            };
            connector.Recognized += (sender, recognitionEventArgs) =>
            {
                Console.WriteLine($"Final speech-to-text result: '{recognitionEventArgs.Result.Text}'");
            };
            connector.SessionStarted += (sender, sessionEventArgs) =>
            {
                Console.WriteLine($"Now Listening! Session started, id={sessionEventArgs.SessionId}");
            };
            connector.SessionStopped += (sender, sessionEventArgs) =>
            {
                Console.WriteLine($"Listening complete. Session ended, id={sessionEventArgs.SessionId}");
            };
            connector.ConnectAsync();

            _quitEvent.WaitOne();
        }
    }
}
