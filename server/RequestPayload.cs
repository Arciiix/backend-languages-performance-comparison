namespace server;

public class RequestPayload
{
    public string Id { get; set; }
    public string TextForSorting { get; set; }
    public TestOfNesting TestOfNesting { get; set; }
}

public class TestOfNesting
{
    public int FibonacciElementToCalculate { get; set; }
}
