﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gene_pool_backend {
  public class BlobStorageHelper {
    private static string containerName = "genepoolcontainer";
    public static BlobStorageHelper Instance { get; set; }

    public static void Init(IConfiguration configuration) {
      Instance = new BlobStorageHelper(configuration);
    }

    private BlobStorageHelper(IConfiguration configuration) {
      string connectionString = configuration["StorageBlob"];
      Console.WriteLine(connectionString);
      blobServiceClient = new BlobServiceClient(connectionString);
    }

    private BlobServiceClient blobServiceClient;

    private string mp4file;
    private string wavfile;
    private string wavname;

    public async Task<dynamic> UploadLinkToBlobAsync(string url) {
      try {
        string mp4filename = Guid.NewGuid().ToString();
        string wavfilename = Guid.NewGuid().ToString();

        var folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        mp4file = Path.Combine(folder, $"{mp4filename}.mp4");
        wavfile = Path.Combine(folder, $"{wavfilename}.wav");
        wavname = $"{wavfilename}.wav";

        File.Delete(mp4file);
        File.Delete(wavfile);

        string ffmpegPath = await DownloadFile("ffmpeg.exe");

        Debug.WriteLine("I got here 1");

        FileHelper.SaveVideoToDisk(url, mp4file);
        dynamic res = FileHelper.ToWavFormat(mp4file, wavfile, ffmpegPath);

        if (res.GetType() != typeof(int) || res != 0) {
          return res;
        }

        // Create the container and return a container client object
        BlobContainerClient containerClient;
        try {
          containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
        } catch {
          containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        Debug.WriteLine("I got here 2");

        // Get a reference to a blob
        BlobClient blobClient = containerClient.GetBlobClient(wavname);

        Debug.WriteLine("I got here 3");

        Debug.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

        try {
          using FileStream uploadFileStream = File.OpenRead(wavfile);
          await blobClient.UploadAsync(uploadFileStream, true);
          uploadFileStream.Close();
        } catch {
          return 3;
        }

        File.Delete(mp4file);
        File.Delete(wavfile);

        return 0;
      } catch {
        return 1;
      }
    }

    public async Task UploadFileToBlobAsync(Microsoft.AspNetCore.Http.IFormFile file) {

      // Create the container and return a container client object
      BlobContainerClient containerClient;
      try {
        containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
      } catch {
        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      }

      string fileName = $"hellofile.wav";

      // Get a reference to a blob
      BlobClient blobClient = containerClient.GetBlobClient(fileName);

      Debug.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

      // Open the file and upload its data
      using (var stream = file.OpenReadStream()) {
        await blobClient.UploadAsync(stream, true);
      }
    }

    public async Task<string[]> TranscribeBlobAsync() {
      // Create the container and return a container client object
      BlobContainerClient containerClient;
      try {
        containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
      } catch {
        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      }

      Console.WriteLine($"Transcribing {wavname}");

      BlobDownloadInfo download;
      try {
        BlobClient blobClient = containerClient.GetBlobClient(wavname);
        download = await blobClient.DownloadAsync();
      } catch {
        return new string[1];
      }

      return await TranscriberHelper.TranscribeBinaryReader(new BinaryReader(download.Content));
    }

    public async Task<string> DownloadFile(string fileName) {
      // Create the container and return a container client object
      BlobContainerClient containerClient;
      try {
        containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
      } catch {
        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      }

      BlobClient blobClient = containerClient.GetBlobClient(fileName);
      BlobDownloadInfo download = await blobClient.DownloadAsync();
      string downloadFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
      string ffmpegPath = Path.Combine(downloadFilePath, fileName);

      using (FileStream downloadFileStream = File.OpenWrite(ffmpegPath)) {
        await download.Content.CopyToAsync(downloadFileStream);
        downloadFileStream.Close();
      }

      return ffmpegPath;
    }
  }
}
