using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace gene_pool_backend {
  public static class FileHelper {
    private static string PathToFfmpeg = "ffmpeg.exe";

    public static void ToWavFormat(string pathToMp4, string pathToWav) {
      var ffmpeg = new Process {
        StartInfo = { UseShellExecute = false, RedirectStandardError = true, FileName = PathToFfmpeg }
      };

      var arguments =
          String.Format(
              @"-i ""{0}"" ""{1}""",
              pathToMp4, pathToWav);

      ffmpeg.StartInfo.Arguments = arguments;

      try {
        if (!ffmpeg.Start()) {
          Console.WriteLine("Error starting");
          return;
        }
        var reader = ffmpeg.StandardError;
        string line;
        while ((line = reader.ReadLine()) != null) {
          Console.WriteLine(line);
        }
      } catch (Exception exception) {
        Console.WriteLine(exception.ToString());
        return;
      }

      ffmpeg.Close();
    }

    public static void SaveVideoToDisk(string link, string fileName) {
      var youTube = YouTube.Default; // starting point for YouTube actions
      var video = youTube.GetVideo(link); // gets a Video object with info about the video
      File.WriteAllBytes(fileName, video.GetBytes());
    }
  }
}
