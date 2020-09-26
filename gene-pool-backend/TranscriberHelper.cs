using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gene_pool_backend {
  public static class TranscriberHelper {
    public static async Task<string> TranscribeBinaryReader(BinaryReader reader) {
      var config = SpeechConfig.FromSubscription("0afaff0095f946eaa101f44563f3c341", "eastus2");

      var stopRecognition = new TaskCompletionSource<int>();

      StringBuilder sb = new StringBuilder();

      using (var audioInput = WavHelper.OpenWavFile(reader)) {
        using (var recognizer = new SpeechRecognizer(config, audioInput)) {
          recognizer.Recognized += (s, e) => {
            if (e.Result.Reason == ResultReason.RecognizedSpeech) {
              Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
              sb.Append(e.Result.Text);
            } else if (e.Result.Reason == ResultReason.NoMatch) {
              Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
          };

          recognizer.Canceled += (s, e) => {
            Console.WriteLine($"CANCELED: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error) {
              Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
              Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
              Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            stopRecognition.TrySetResult(0);
          };

          recognizer.SessionStarted += (s, e) => {
            Console.WriteLine("\nSession started event.");
          };

          recognizer.SessionStopped += (s, e) => {
            Console.WriteLine("\nSession stopped event.");
            Console.WriteLine("\nStop recognition.");
            stopRecognition.TrySetResult(0);
          };

          await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

          Task.WaitAny(new[] { stopRecognition.Task });

          await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
      }

      return sb.ToString();
    }
  }
}
