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
  public static class BinaryWriterExtensions {
    private const int HeaderSize = 44;

    private const int Hz = 16000; //frequency or sampling rate

    private const float RescaleFactor = 32767; //to convert float to Int16

    public static void AppendWaveData<T>(this T stream, float[] buffer)
       where T : Stream {
      if (stream.Length > HeaderSize) {
        stream.Seek(0, SeekOrigin.End);
      } else {
        stream.SetLength(HeaderSize);
        stream.Position = HeaderSize;
      }

      // rescale
      var floats = Array.ConvertAll(buffer, x => (short)(x * RescaleFactor));

      // Copy to bytes
      var result = new byte[floats.Length * sizeof(short)];
      Buffer.BlockCopy(floats, 0, result, 0, result.Length);

      // write to stream
      stream.Write(result, 0, result.Length);

      // Update Header
      UpdateHeader(stream);
    }

    public static void UpdateHeader(Stream stream) {
      var writer = new BinaryWriter(stream);

      writer.Seek(0, SeekOrigin.Begin);

      writer.Write(Encoding.ASCII.GetBytes("RIFF")); //RIFF marker. Marks the file as a riff file. Characters are each 1 byte long. 
      writer.Write((int)(writer.BaseStream.Length - 8)); //file-size (equals file-size - 8). Size of the overall file - 8 bytes, in bytes (32-bit integer). Typically, you'd fill this in after creation.
      writer.Write(Encoding.ASCII.GetBytes("WAVE")); //File Type Header. For our purposes, it always equals "WAVE".
      writer.Write(Encoding.ASCII.GetBytes("fmt ")); //Mark the format section. Format chunk marker. Includes trailing null. 
      writer.Write(16); //Length of format data.  Always 16. 
      writer.Write((short)1); //Type of format (1 is PCM, other number means compression) . 2 byte integer. Wave type PCM
      writer.Write((short)2); //Number of Channels - 2 byte integer
      writer.Write(Hz); //Sample Rate - 32 byte integer. Sample Rate = Number of Samples per second, or Hertz.
      writer.Write(Hz * 2 * 1); // sampleRate * bytesPerSample * number of channels, here 16000*2*1.
      writer.Write((short)(1 * 2)); //channels * bytesPerSample, here 1 * 2  // Bytes Per Sample: 1=8 bit Mono,  2 = 8 bit Stereo or 16 bit Mono, 4 = 16 bit Stereo
      writer.Write((short)16); //Bits per sample (BitsPerSample * Channels) ?? should be 8???
      writer.Write(Encoding.ASCII.GetBytes("data")); //"data" chunk header. Marks the beginning of the data section.    
      writer.Write((int)(writer.BaseStream.Length - HeaderSize)); //Size of the data section. data-size (equals file-size - 44). or NumSamples * NumChannels * bytesPerSample ??        
    }
  } //end of class

  public static class FileHelpers {
    public static byte[] ReadToEnd(Stream stream) {
      long originalPosition = 0;

      if (stream.CanSeek) {
        originalPosition = stream.Position;
        stream.Position = 0;
      }

      byte[] readBuffer = new byte[4096];

      int totalBytesRead = 0;
      int bytesRead;

      while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0) {
        totalBytesRead += bytesRead;

        if (totalBytesRead == readBuffer.Length) {
          int nextByte = stream.ReadByte();
          if (nextByte != -1) {
            byte[] temp = new byte[readBuffer.Length * 2];
            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
            readBuffer = temp;
            totalBytesRead++;
          }
        }
      }

      byte[] buffer = readBuffer;
      if (readBuffer.Length != totalBytesRead) {
        buffer = new byte[totalBytesRead];
        Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
      }
      return buffer;
    }

    public static float[] ConvertByteToFloat(byte[] array) {
      float[] floatArr = new float[array.Length / 4];
      for (int i = 0; i < floatArr.Length; i++) {
        if (BitConverter.IsLittleEndian) {
          Array.Reverse(array, i * 4, 4);
        }
        floatArr[i] = BitConverter.ToSingle(array, i * 4);
      }
      return floatArr;
    }

    public static void ConvertToWAVOLD(byte [] video) {
      // contentAsByteArray consists of video bytes
      MemoryStream contentAsMemoryStream = new MemoryStream(video);

      using (WaveStream pcmStream =
          WaveFormatConversionStream.CreatePcmStream(
              new StreamMediaFoundationReader(contentAsMemoryStream))) {
        WaveStream blockAlignReductionStream = new BlockAlignReductionStream(pcmStream);

        // Do something with the wave stream
        using (var stream = new FileStream("hello.wav", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
          stream.AppendWaveData(ConvertByteToFloat(ReadToEnd(blockAlignReductionStream)));
        }
      }
    }

    public static string PathToFfmpeg { get; set; }

    public static void ToWavFormat(string pathToMp4, string pathToWav) {
      PathToFfmpeg = "ffmpeg.exe";

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

    public static void SaveVideoToDisk(string link) {
      var youTube = YouTube.Default; // starting point for YouTube actions
      var video = youTube.GetVideo(link); // gets a Video object with info about the video
      File.WriteAllBytes("hello.mp4", video.GetBytes());
    }

    public static void CreateEmptyFile(string filename) {
      File.Create(filename).Dispose();
    }
  }
}
