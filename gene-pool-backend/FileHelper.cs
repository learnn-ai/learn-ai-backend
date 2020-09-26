using Microsoft.CognitiveServices.Speech.Audio;
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

    public static int ToWavFormat(string pathToMp4, string pathToWav) {
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
          return -1;
        }
        var reader = ffmpeg.StandardError;
        string line;
        while ((line = reader.ReadLine()) != null) {
          Console.WriteLine(line);
        }
      } catch (Exception exception) {
        Console.WriteLine(exception.ToString());
        return -2;
      }

      ffmpeg.Close();

      return 0;
    }

    public static bool SaveVideoToDisk(string link, string fileName) {
      try {
        var youTube = YouTube.Default; // starting point for YouTube actions
        var video = youTube.GetVideo(link); // gets a Video object with info about the video
        File.WriteAllBytes(fileName, video.GetBytes());

        return true;
      } catch {
        return false;
      }
    }
  }

  public class WavHelper {
    // SRC: https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/helper.cs
    public static AudioConfig OpenWavFile(string filename) {
      BinaryReader reader = new BinaryReader(File.OpenRead(filename));
      return OpenWavFile(reader);
    }

    public static AudioConfig OpenWavFile(BinaryReader reader) {
      AudioStreamFormat format = readWaveHeader(reader);
      return AudioConfig.FromStreamInput(new BinaryAudioStreamReader(reader), format);
    }

    public static BinaryAudioStreamReader CreateWavReader(string filename) {
      BinaryReader reader = new BinaryReader(File.OpenRead(filename));
      AudioStreamFormat format = readWaveHeader(reader);
      return new BinaryAudioStreamReader(reader);
    }

    public static BinaryAudioStreamReader CreateBinaryFileReader(string filename) {
      BinaryReader reader = new BinaryReader(File.OpenRead(filename));
      return new BinaryAudioStreamReader(reader);
    }

    public static AudioStreamFormat readWaveHeader(BinaryReader reader) {
      char[] data = new char[4];
      reader.Read(data, 0, 4);
      Trace.Assert((data[0] == 'R') && (data[1] == 'I') && (data[2] == 'F') && (data[3] == 'F'), "Wrong wav header");

      long fileSize = reader.ReadInt32();

      reader.Read(data, 0, 4);
      Trace.Assert((data[0] == 'W') && (data[1] == 'A') && (data[2] == 'V') && (data[3] == 'E'), "Wrong wav tag in wav header");

      reader.Read(data, 0, 4);
      Trace.Assert((data[0] == 'f') && (data[1] == 'm') && (data[2] == 't') && (data[3] == ' '), "Wrong format tag in wav header");

      var formatSize = reader.ReadInt32();
      var formatTag = reader.ReadUInt16();
      var channels = reader.ReadUInt16();
      var samplesPerSecond = reader.ReadUInt32();
      var avgBytesPerSec = reader.ReadUInt32();
      var blockAlign = reader.ReadUInt16();
      var bitsPerSample = reader.ReadUInt16();

      if (formatSize > 16)
        reader.ReadBytes((int)(formatSize - 16));

      reader.Read(data, 0, 4);
      // Trace.Assert((data[0] == 'd') && (data[1] == 'a') && (data[2] == 't') && (data[3] == 'a'), "Wrong data tag in wav");

      int dataSize = reader.ReadInt32();

      return AudioStreamFormat.GetWaveFormatPCM(samplesPerSecond, (byte)bitsPerSample, (byte)channels);
    }
  }

  public sealed class BinaryAudioStreamReader : PullAudioInputStreamCallback {
    private System.IO.BinaryReader _reader;

    public BinaryAudioStreamReader(System.IO.BinaryReader reader) {
      _reader = reader;
    }

    public BinaryAudioStreamReader(System.IO.Stream stream)
        : this(new System.IO.BinaryReader(stream)) {
    }

     public override int Read(byte[] dataBuffer, uint size) {
      return _reader.Read(dataBuffer, 0, (int)size);
    }

    protected override void Dispose(bool disposing) {
      if (disposed) {
        return;
      }

      if (disposing) {
        _reader.Dispose();
      }

      disposed = true;
      base.Dispose(disposing);
    }

    private bool disposed = false;
  }
}
