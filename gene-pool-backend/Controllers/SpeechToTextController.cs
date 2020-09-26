using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using System.Text;
using NAudio.Wave;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using VideoLibrary;
using MediaToolkit.Model;
using NAudio.Utils;
using Newtonsoft.Json;

namespace gene_pool_backend.Controllers {
  public class MyFile {
    public string test { get; set; }
    public IFormFile file { get; set; }
  }

  [Route("api/[controller]")]
  [ApiController]
  public class SpeechToTextController : ControllerBase {
    [HttpPost]
    [Route("wav_file_transcribe")]
    public async Task<IActionResult> WavFileTranscribe([FromForm] MyFile files) {
      Console.WriteLine(files);
      Console.WriteLine(files.test);
      Console.WriteLine(files.file);

      var config = SpeechConfig.FromSubscription("0afaff0095f946eaa101f44563f3c341", "eastus2");
      var stopRecognition = new TaskCompletionSource<int>();

      Console.WriteLine(Path.GetFullPath("test.wav"));

      StringBuilder sb = new StringBuilder();

      // using (var audioConfig = AudioConfig.FromStreamInput(audioStream)) {
      using (var audioConfig = AudioConfig.FromWavFileInput(Path.GetFullPath("helloworld.wav"))) {
        using (var recognizer = new SpeechRecognizer(config, audioConfig)) {
          // Subscribes to events.
          recognizer.Recognized += (s, e) =>
          {
            if (e.Result.Reason == ResultReason.RecognizedSpeech) {
              Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
              sb.Append(e.Result.Text);
            } else if (e.Result.Reason == ResultReason.NoMatch) {
              Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
          };

          recognizer.Canceled += (s, e) =>
          {
            Console.WriteLine($"CANCELED: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error) {
              Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
              Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
              Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            stopRecognition.TrySetResult(0);
          };

          recognizer.SessionStarted += (s, e) =>
          {
            Console.WriteLine("\n    Session started event.");
          };

          recognizer.SessionStopped += (s, e) =>
          {
            Console.WriteLine("\n    Session stopped event.");
            Console.WriteLine("\nStop recognition.");
            stopRecognition.TrySetResult(0);
          };

          // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
          await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

          // Waits for completion.
          // Use Task.WaitAny to keep the task rooted.
          Task.WaitAny(new[] { stopRecognition.Task });

          // Stops recognition.
          await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
      }

      Console.WriteLine(sb.ToString());

      return Ok();
    }

    [HttpPost]
    [Route("transcribe_link")]
    public async Task<IActionResult> LinkToWav(dynamic request) {
      var body = JsonConvert.DeserializeObject<dynamic>(request.ToString());

      try {
        int res = await BlobStorageHelper.Instance.UploadLinkToBlobAsync(body.url.ToString());
        if (res != 0) {
          return BadRequest(res);
        }
      } catch {
        return BadRequest("Error during upload");
      }

      try {
        string[] res = await BlobStorageHelper.Instance.TranscribeBlobAsync();
        if (res.Length == 1) {
          return BadRequest("Error with reading file");
        }
        return Ok(res);
      } catch {
        return BadRequest("Unknown error during transcription");
      }
    }
  }
}
