namespace server;

public record Result(TimeSpan Overall, TimeSpan Fibonnaci, TimeSpan Sorting);

public class ResultRaw
{
    public string Id { get; set; }
    public int MillisecondsForFibonnaci { get; set; }
    public int MillisecondsForSorting { get; set; }

}
