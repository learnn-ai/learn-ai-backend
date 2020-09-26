using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gene_pool_backend {
  public static class TranscriberHelper {
    public static async Task<string[]> TranscribeBinaryReader(BinaryReader reader) {
      var config = SpeechConfig.FromSubscription("0afaff0095f946eaa101f44563f3c341", "eastus2");

      var stopRecognition = new TaskCompletionSource<int>();

      StringBuilder sb = new StringBuilder();
      string log;
      StringBuilder logs = new StringBuilder();

      using (var audioInput = WavHelper.OpenWavFile(reader)) {
        using (var recognizer = new SpeechRecognizer(config, audioInput)) {
          recognizer.Recognized += (s, e) => {
            if (e.Result.Reason == ResultReason.RecognizedSpeech) {
              Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
              sb.Append(e.Result.Text);
            } else if (e.Result.Reason == ResultReason.NoMatch) {
              log = $"NOMATCH: Speech could not be recognized.";
              logs.Append(log);
            }
          };

          recognizer.Canceled += (s, e) => {
            log = $"CANCELED: Reason={e.Reason}";
            logs.Append(log);

            if (e.Reason == CancellationReason.Error) {
              log = $"CANCELED: ErrorCode={e.ErrorCode}\nCANCELED: ErrorDetails={e.ErrorDetails}\nCANCELED: Did you update the subscription info?";
              logs.Append(log);
            }

            stopRecognition.TrySetResult(0);
          };

          recognizer.SessionStarted += (s, e) => {
            log = "\nSession started event.";
            logs.Append(log);
          };

          recognizer.SessionStopped += (s, e) => {
            log = "\nSession stopped event.\nStop recognition.";
            logs.Append(log);
            stopRecognition.TrySetResult(0);
          };

          await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

          Task.WaitAny(new[] { stopRecognition.Task });

          await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
      }

      string[] res = new string[2];
      res[0] = sb.ToString();
      res[1] = logs.ToString();

      return res;
    }
  }
}
