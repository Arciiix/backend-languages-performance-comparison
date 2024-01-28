
using csharp;
using csharp.Models;
using Newtonsoft.Json;
using Websocket.Client;

var websocektURL = new Uri("ws://localhost:8181");

var exitEvent = new ManualResetEvent(false);



using var client = new WebsocketClient(websocektURL);

int CalculateFibonacci(int n)
{
    if (n == 0) return 0;
    if (n == 1) return 1;

    int number = n - 1;
    int[] series = new int[number + 1];
    series[0] = 0;
    series[1] = 1;
    for (int i = 2; i <= number; i++)
    {
        series[i] = series[i - 2] + series[i - 1];
    }
    return series[number];
}

client.MessageReceived.Subscribe(async (msg) =>
{
    // JSON deserialization
    var data = JsonConvert.DeserializeObject<RequestPayload>(msg.Text!)!;

    // Simple database query
    DateTime now = DateTime.Now;
    var random = new Random();
    using var dbContext = new TestContext();
    var row = await dbContext.Examples.FindAsync(random.Next(10));
    TimeSpan timeForDb = DateTime.Now - now;

    // Simple loop
    now = DateTime.Now;
    CalculateFibonacci(data.TestOfNesting.FibonacciElementToCalculate);
    TimeSpan timeForFibonacci = DateTime.Now - now;

    // Filter the string
    now = DateTime.Now;
    int[] numbersOnly = data.TextForSorting.Where(char.IsDigit).Select(c => int.Parse(c.ToString())).ToArray();

    // Sort an array
    int[] numbersSorted = numbersOnly.OrderBy(n => n).ToArray();

    TimeSpan timeForSorting = DateTime.Now - now;

    // Serialize and send the result
    var result = new Result()
    {
        Id = data.Id,
        MillisecondsForDb = ((int)timeForDb.TotalMilliseconds),
        MillisecondsForFibonacci = ((int)timeForFibonacci.TotalMilliseconds),
        MillisecondsForSorting = ((int)timeForSorting.TotalMilliseconds)
    };
    var resultSerialized = JsonConvert.SerializeObject(result);
    await client.SendInstant(resultSerialized);
});

await client.StartOrFail();
Console.WriteLine("Press any key to start");
Console.ReadKey();
client.Send("csharp");

exitEvent.WaitOne();