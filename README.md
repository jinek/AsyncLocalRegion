# Async Local Region

Control the lifetime of [`AsyncLocal<T>`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1) objects using the [`IDisposable`](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable) pattern. This package enables scoped lifespan management of `AsyncLocal` values within asynchronous code blocks to ensure values are accessible only during the defined region.

## Features
- Define a scoped region for an `AsyncLocal<T>` value.
- Retrieve the value in any inner tasks or method calls.
- Automatically dispose of the scoped region to prevent accidental value leakage.

## Usage

Here's an example demonstrating how to define a region for an `AsyncLocal<string>` and retrieve it within an asynchronous method:
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
## Installation
```
dotnet add package AsyncLocalRegion
```
## Use Cases
**Passing Data Through Unowned Layers of Code:**
- **Serialization Hooks**:
  Use `AsyncLocalRegion` to pass additional context to the `[OnSerialize]` method in serialization processes. This is particularly useful in scenarios where serializers do not natively support passing custom data. For example, you can store a user's permission level in `AsyncLocalRegion` before serialization begins, and access this data within the `OnSerialize` method to decide which properties of an object should be serialized based on the user's permissions.

  ```csharp
  private static AsyncLocalRegion<string> UserPermission = new();

  public void PrepareSerializationForAdmin()
  {
      using (UserPermission.StartRegion("Admin"))
      {
          SerializeData();
      }
  }

  [OnSerialize]
  private void OnSerialize()
  {
      if (UserPermission.CurrentValue == "Admin")
      {
          // Serialize sensitive data
      }
  }
  ```
- **Data Mapping**:
  When mapping data from one object to another, AsyncLocalRegion can be used to provide context that guides how data should be mapped, especially useful in libraries where method signatures cannot be altered. For instance, you might need to adapt data differently based on the region or locale, which could be stored in AsyncLocalRegion and accessed during the mapping process.
  ```csharp
  private static AsyncLocalRegion<string> MappingContext = new();

  public void ConfigureMappingForEU()
  {
    using (MappingContext.StartRegion("EU"))
    {
      MapData();
    }
  }

  private void MapData()
  {
    if (MappingContext.CurrentValue == "EU")
    {
      // Map data according to European standards
    }
  }
  ```