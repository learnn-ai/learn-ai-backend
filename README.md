# gene-pool-backend

This ASP.NET based back-end controls the data flow for transcribing YouTube videos into a text-based format.

## How to run
In order to run this repository, paste in your Azure Storage connection string into appsettings.json, run the application in debug mode, and send a POST request with a JSON body to {{url}}/api/speechtotext/transcribe_link.

Example:
{
    "url": "https://www.youtube.com/watch?v=..."
}

Note that there is a near 1-to-1 transcribing time for video length to API response times. We convert a .mp4 to a .wav and feed it to Azure, which parses it in real time to understand speech.

## Todo
- Update the back-end with cleaner code
- Rework CosmosDB integration to utilize proper querying methods and protect against injections
