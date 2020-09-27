# gene-pool-backend

This ASP.NET based back-end controls the data flow for transcribing YouTube videos into a text-based format.

In order to run this repository, paste in your Azure Storage connection string into appsettings.json, run the application in debug mode, and send a POST request with a JSON body to {{url}}/api/speechtotext/transcribe_link.

Example:
{
    "url": "https://www.youtube.com/watch?v=..."
}
