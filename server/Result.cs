﻿namespace server;

public record Result(string Id, TimeSpan Overall, TimeSpan Db, TimeSpan Fibonnaci, TimeSpan Sorting);

public class ResultRaw
{
    public string Id { get; set; }
    public int MillisecondsForDb { get; set; }
    public int MillisecondsForFibonnaci { get; set; }
    public int MillisecondsForSorting { get; set; }

}
