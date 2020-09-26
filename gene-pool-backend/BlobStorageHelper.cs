using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gene_pool_backend {
  public static class BlobStorageHelper {
    private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=genepoolstorage;AccountKey=gYC3jnsvdZCSxQJH4hTn2kpy9SyDW5bpfB5KIjB7D0SPMu0GG7y/mlrJNFrAGi56kadHW+VDwsxoYKvb3eaCAw==;EndpointSuffix=core.windows.net";
    private static string containerName = "genepoolcontainer";

    private static BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

    public static async Task UploadLinkToBlobAsync(string url) {
      File.Delete("hello.mp4");
      File.Delete("hello.wav");

      FileHelper.SaveVideoToDisk(url, "hello.mp4");
      FileHelper.ToWavFormat("hello.mp4", "hello.wav");

      // Create the container and return a container client object
      BlobContainerClient containerClient;
      try {
        containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
      } catch {
        containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      }

      string fileName = $"hello.wav";

      // Get a reference to a blob
      BlobClient blobClient = containerClient.GetBlobClient(fileName);

      Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

      using FileStream uploadFileStream = File.OpenRead("hello.wav");
      await blobClient.UploadAsync(uploadFileStream, true);
      uploadFileStream.Close();

      File.Delete("hello.mp4");
      File.Delete("hello.wav");
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

      Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

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
