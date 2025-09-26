# X Client Transaction ID Generator

A C# library for generating cryptographic transaction IDs used by the X (formerly Twitter) web client. This library extracts and processes animation data and cryptographic keys from X's homepage to generate valid transaction identifiers.

## Features

- Extracts cryptographic keys from X homepage HTML
- Processes animation frame data for transaction generation
- Generates SHA256-based transaction IDs with XOR encryption
- Supports both direct usage and factory pattern initialization

## Prerequisites

- .NET 8.0 or later
- HtmlAgilityPack NuGet package

## Installation

1. Clone this repository:
```bash
git clone <repository-url>
cd cs-x-client-transaction-id
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

## Usage

### Basic Usage

```csharp
using HtmlAgilityPack;
using XClientTransactionId;

// Load X homepage HTML
var document = new HtmlDocument();
document.LoadHtml(htmlContent);

// Create and initialize ClientTransaction
var transaction = new ClientTransaction(document);
await transaction.InitializeAsync();

// Generate transaction ID
string transactionId = await transaction.GenerateTransactionIdAsync(
    method: "POST",
    path: "/api/endpoint"
);

Console.WriteLine(transactionId);
```

### Factory Pattern

```csharp
// Using the factory method for one-step initialization
var transaction = await ClientTransaction.CreateAsync(document);
string transactionId = await transaction.GenerateTransactionIdAsync("POST", "/api/endpoint");
```

### Advanced Usage

```csharp
// Generate transaction ID with custom parameters
string transactionId = await transaction.GenerateTransactionIdAsync(
    method: "GET",
    path: "/api/users/lookup",
    response: customDocument,      // Optional: custom HTML document
    key: customKey,               // Optional: custom base64 key
    animationKey: customAnimKey,  // Optional: custom animation key
    timeNow: customTimestamp      // Optional: custom timestamp
);
```

## Command Line Usage

The included Program.cs provides a command-line interface:

```bash
# Requires x.com.html file in parent directory
dotnet run "/api/endpoint"
```

## How It Works

1. **Key Extraction**: Extracts base64-encoded key from the `twitter-site-verification` meta tag
2. **Animation Data**: Fetches and processes animation frame data from X's ondemand JavaScript files
3. **Cryptographic Processing**: 
   - Combines HTTP method, path, timestamp, and animation data
   - Generates SHA256 hash of the combined data
   - XOR encrypts the result with extracted keys
   - Encodes as base64 string

## API Reference

### ClientTransaction Class

#### Constructor
- `ClientTransaction(HtmlDocument homePageDocument)` - Creates a new instance

#### Methods
- `InitializeAsync()` - Initializes the instance with extracted keys and animation data
- `CreateAsync(HtmlDocument)` - Static factory method that creates and initializes an instance
- `GenerateTransactionIdAsync(string method, string path, ...)` - Generates a transaction ID

#### Parameters for GenerateTransactionIdAsync
- `method` (required) - HTTP method (e.g., "GET", "POST")
- `path` (required) - API endpoint path
- `response` (optional) - Custom HTML document
- `key` (optional) - Custom base64 key
- `animationKey` (optional) - Custom animation key  
- `timeNow` (optional) - Custom timestamp

## Error Handling

The library throws exceptions for various error conditions:
- Missing or invalid HTML document
- Network errors when fetching ondemand files
- Missing cryptographic keys or animation data
- Uninitialized instance usage

Wrap method calls in try-catch blocks for proper error handling:

```csharp
try
{
    var transaction = await ClientTransaction.CreateAsync(document);
    var id = await transaction.GenerateTransactionIdAsync("POST", "/api/endpoint");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Dependencies

- **HtmlAgilityPack** (v1.11.54) - HTML parsing and manipulation
- **.NET 8.0** - Target framework
- **System.Security.Cryptography** - SHA256 hashing
- **System.Net.Http** - HTTP client for fetching ondemand files

## License

This project is provided as-is for educational and research purposes.