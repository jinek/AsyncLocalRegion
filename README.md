# Async Local Region

Manages lifetime of AsyncLocal<T> using IDisposable pattern.

1) Provide a value while defining a region
2) Retrieve the value in any of inner Tasks/calls
3) Dispose the region to protect value from accidental usage

```c#
private static AsyncLocalRegion<string> MyParameter = new();

public static async void Run()
{
    using (MyParameter.StartRegion("Value for this Task only"))
    {
        await RetrieveValue();
    }
}

private static async Task RetrieveValue()
{
    // prints "Value for this Task only"
    Console.WriteLine(MyParameter.CurrentValue); 
}
```