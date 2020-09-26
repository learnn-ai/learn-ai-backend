using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gene_pool_backend {
  public static class BlobStorageHelper {
    private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=genepoolstorage;AccountKey=gYC3jnsvdZCSxQJH4hTn2kpy9SyDW5bpfB5KIjB7D0SPMu0GG7y/mlrJNFrAGi56kadHW+VDwsxoYKvb3eaCAw==;EndpointSuffix=core.windows.net";
    private static string containerName = "genepoolcontainer";

    private static BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

    private static string mp4file;
    private static string wavfile;

    public static async Task UploadLinkToBlobAsync(string url) {
      var folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
      mp4file = Path.Combine(folder, "hello.mp4");
      wavfile = Path.Combine(folder, "hello.wav");

      File.Delete(mp4file);
      File.Delete(wavfile);

      Debug.WriteLine("I got here 1");

      FileHelper.SaveVideoToDisk(url, mp4file);
      FileHelper.ToWavFormat(mp4file, wavfile);

      // Create the container and return a container client object
      BlobContainerClient containerClient;
      try {
        containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
      } catch {
        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      }

      Debug.WriteLine("I got here 2");

      string fileName = $"hello.wav";

      // Get a reference to a blob
      BlobClient blobClient = containerClient.GetBlobClient(fileName);

      Debug.WriteLine("I got here 3");

      Debug.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

      using FileStream uploadFileStream = File.OpenRead(wavfile);
      await blobClient.UploadAsync(uploadFileStream, true);
      uploadFileStream.Close();

      Debug.WriteLine("I got here 4");

      File.Delete(mp4file);
      File.Delete(wavfile);
    }

    public static async Task UploadFileToBlobAsync(Microsoft.AspNetCore.Http.IFormFile file) {

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

    public static async Task<string> TranscribeBlobAsync() {
      // Create the container and return a container client object
      BlobContainerClient containerClient;
      try {
        containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
      } catch {
        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      }

      BlobClient blobClient = containerClient.GetBlobClient($"hello.wav");
      BlobDownloadInfo download = await blobClient.DownloadAsync();

      return await TranscriberHelper.TranscribeBinaryReader(new BinaryReader(download.Content));
    }
  }
}
