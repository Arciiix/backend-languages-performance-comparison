using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using server;
using System.Collections.Concurrent;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

var server = new WebSocketServer("ws://0.0.0.0:8181");
using ILoggerFactory factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new TimestampLoggerProvider());
    //builder.AddConsole();
});
ILogger logger = factory.CreateLogger("Back-end benchmark server");

DateTime? endDate = null;

int testingTimeSeconds = 5;

if (int.TryParse(args.FirstOrDefault(), out testingTimeSeconds))
{
    logger.LogInformation("Got testing time in seconds from args: {TestingTimeSeconds}", testingTimeSeconds);
}

TimeSpan testingTime = TimeSpan.FromSeconds(testingTimeSeconds);

List<RequestPayload> requestsToSend = [];
ConcurrentDictionary<string, DateTime> requests = [];

string nameOfTestedTechnology = "Unknown";

ConcurrentQueue<Result> results = [];

Random random = new Random();
const string chars = "abcdefghijklmnopqrstuvwxyz012345678901234567890123456789";

int totalNumberOfRequests = 0;
int numberOfReceivedRequests = 0;

int indexOfRequest = 0;
int indexOfCurrentRequest = 0;


void SendMessage(IWebSocketConnection ws)
{
    RequestPayload toSend = requestsToSend[indexOfCurrentRequest];
    indexOfCurrentRequest++;
    if (indexOfCurrentRequest >= requestsToSend.Count)
    {
        indexOfCurrentRequest = 0;
    }


    var toSendSerialized = JsonConvert.SerializeObject(toSend, Formatting.None, new JsonSerializerSettings()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    });
    requests[toSend.Id] = DateTime.Now;
    ws.Send(toSendSerialized);
}

RequestPayload GenerateRequest()
{
    indexOfRequest++;
    StringBuilder sb = new StringBuilder();

    for (int i = 0; i < 10_000; i++)
    {
        sb.Append(chars[random.Next(chars.Length)]);
    }


    return new RequestPayload()
    {
        Id = indexOfRequest.ToString(),
        TextForSorting = sb.ToString(),
        TestOfNesting = new TestOfNesting() { FibonacciElementToCalculate = random.Next(50) }
    };
}

void ExportResults()
{
    for (int i = 0; i < 5; i++)
    {
        Console.WriteLine(Environment.NewLine);
    }

    double percentageOfResponses = (numberOfReceivedRequests * 1.0 / totalNumberOfRequests) * 100;
    double meanTimePerRequest = results.Select(e => e.Overall.TotalMilliseconds).Average();
    double requestsPerSecond = (numberOfReceivedRequests * 1.0) / testingTime.Seconds;

    List<string> comments = [];

    comments.Add($"Technology: {nameOfTestedTechnology}");
    comments.Add($"Received responses: {numberOfReceivedRequests}");
    comments.Add($"Sent requests: {totalNumberOfRequests}");
    comments.Add($"Received responses [%]: {percentageOfResponses}");
    comments.Add($"Mean time per request [ms]: {meanTimePerRequest}");
    comments.Add($"Requests per seconds: {requestsPerSecond}");

    logger.LogInformation("----- TEST COMPLETE -----");
    logger.LogInformation("{Results}", string.Join(Environment.NewLine, comments));

    // Get the current directory
    string currentDirectory = Directory.GetCurrentDirectory();

    // Define the file name
    string fileName = $"TEST_{nameOfTestedTechnology}_{DateTime.Now.ToString("yyyyMMddmmHHss")}.csv";

    // Combine the current directory path and the file name
    string filePath = Path.Combine(currentDirectory, fileName);

    logger.LogInformation("Dumping at: {FileLocation}", filePath);

    try
    {
        // Create a StreamWriter to write to the CSV file
        using var writer = new StreamWriter(filePath);

        comments.ForEach(e => writer.WriteLine($"#{e}"));

        // Write header
        writer.WriteLine("Id,TimeTakenMs,TimeTakenForDbMs,TimeTakenFibonnaciMs,TimeTakenSortMs");

        // Write each person's data
        foreach (Result result in results)
        {
            writer.WriteLine($"{result.Id},{(int)result.Overall.TotalMilliseconds},{(int)result.Db.TotalMilliseconds},{(int)result.Fibonnaci.TotalMilliseconds},{(int)result.Sorting.TotalMilliseconds}");
        }


        logger.LogInformation("CSV file created successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError("{Exception}", ex.Message);
    }



    Environment.Exit(0);
}

void StartSending(IWebSocketConnection ws)
{
    // First create a list of potential requests to send, to later avoid slowing it down
    for (int i = 0; i < 100_000; i++)
    {
        requestsToSend.Add(GenerateRequest());
    }

    ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
    ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
    ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);

    logger.LogInformation("WorkerThreads: {WorkerThreads}", workerThreads);
    logger.LogInformation("CompletionPortThreads: {CompletionPortThreads}", completionPortThreads);
    logger.LogInformation("MinWorkerThreads: {MinWorkerThreads}", minWorkerThreads);
    logger.LogInformation("MinCompletionPortThreads: {MinCompletionPortThreads}", minCompletionPortThreads);
    logger.LogInformation("MaxWorkerThreads: {MaxWorkerThreads}", maxWorkerThreads);
    logger.LogInformation("MaxCompletionPortThreads: {MaxCompletionPortThreads}", maxCompletionPortThreads);

    logger.LogInformation("Init!");


    endDate = DateTime.Now + testingTime;
    while (endDate > DateTime.Now)
    {
        // If the gap is below 10k requests
        if (totalNumberOfRequests - numberOfReceivedRequests < 500)
        {
            Task.Run(() => SendMessage(ws));
            totalNumberOfRequests++;
        }
        //logger.LogInformation("Waiting");
    }
    logger.LogInformation("Testing worker completed");

    ExportResults();
}

server.Start(ws =>
{
    ws.OnOpen = () =>
    {
        logger.LogInformation("New client!");
    };

    ws.OnMessage = (message) =>
    {
        //Console.WriteLine(message);

        // Starts when received a message
        if (endDate is null)
        {
            logger.LogInformation("Started!");
            nameOfTestedTechnology = message;
            endDate = DateTime.Now + testingTime;
            new Thread(() => StartSending(ws)).Start();


            return;
        }
        if (endDate < DateTime.Now) return;

        numberOfReceivedRequests++;
        var messageParsed = JsonConvert.DeserializeObject<ResultRaw>(message)!;

        results.Enqueue(new Result(
            messageParsed.Id,
            DateTime.Now - requests[messageParsed.Id],
            TimeSpan.FromMilliseconds(messageParsed.MillisecondsForDb),
            TimeSpan.FromMilliseconds(messageParsed.MillisecondsForFibonacci),
            TimeSpan.FromMilliseconds(messageParsed.MillisecondsForSorting)));

        // No print statements because they would slow it down very much (they're a SYSCALL)
    };
});

app.Run();
