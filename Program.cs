using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using XClientTransactionId;

class Program
{
    static async Task Main(string[] args)
    {
        // Get URL from command line arguments
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: dotnet run <url>");
            Environment.Exit(1);
            return;
        }

        string url = args[0];

        try
        {
            // Get Twitter homepage HTML document from local file
            string htmlContent = await File.ReadAllTextAsync("../x.com.html");
            
            // Parse the HTML with HtmlAgilityPack
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            // Create and initialize ClientTransaction instance
            var transaction = new ClientTransaction(document);
            await transaction.InitializeAsync();

            // Generate a transaction ID for the provided URL
            string transactionId = transaction.GenerateTransactionId(
                "POST", // HTTP method
                url     // API path from command line
            );

            // Output the transaction ID
            Console.WriteLine(transactionId);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine("Error: x.com.html file not found");
            Environment.Exit(1);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error fetching ondemand file: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating transaction ID: {ex.Message}");
            Environment.Exit(1);
        }
    }
}