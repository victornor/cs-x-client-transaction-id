using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace XClientTransactionId;

public static class Utils
{
    public static string FloatToHex(double x)
    {
        var result = new List<string>();
        var quotient = (long)Math.Floor(x);
        var fraction = x - quotient;

        while (quotient > 0)
        {
            quotient = (long)Math.Floor(x / 16);
            var remainder = (long)Math.Floor(x - quotient * 16);

            if (remainder > 9)
            {
                result.Insert(0, ((char)(remainder + 55)).ToString());
            }
            else
            {
                result.Insert(0, remainder.ToString());
            }

            x = quotient;
        }

        if (fraction == 0)
        {
            return string.Join("", result);
        }

        result.Add(".");

        while (fraction > 0)
        {
            fraction *= 16;
            var integer = (int)Math.Floor(fraction);
            fraction -= integer;

            if (integer > 9)
            {
                result.Add(((char)(integer + 55)).ToString());
            }
            else
            {
                result.Add(integer.ToString());
            }
        }

        return string.Join("", result);
    }

    public static double IsOdd(int num)
    {
        return num % 2 == 1 ? -1.0 : 0.0;
    }

    public static async Task<HtmlDocument> HandleXMigration()
    {
        using var httpClient = new HttpClient();
        
        httpClient.DefaultRequestHeaders.Add("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "ja");
        httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        httpClient.DefaultRequestHeaders.Add("Sec-CH-UA", 
            "\"Google Chrome\";v=\"135\", \"Not-A.Brand\";v=\"8\", \"Chromium\";v=\"135\"");
        httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
        httpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");

        var response = await httpClient.GetAsync("https://x.com");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch X homepage: {response.StatusCode}");
        }

        var htmlText = await response.Content.ReadAsStringAsync();
        var document = new HtmlDocument();
        document.LoadHtml(htmlText);

        var migrationRedirectionRegex = new Regex(
            @"(http(?:s)?://(?:www\.)?(twitter|x){1}\.com(/x)?/migrate([/?])?tok=[a-zA-Z0-9%\-_]+)+",
            RegexOptions.IgnoreCase);

        var metaRefresh = document.DocumentNode.SelectSingleNode("//meta[@http-equiv='refresh']");
        var metaContent = metaRefresh?.GetAttributeValue("content", "") ?? "";

        var migrationRedirectionMatch = migrationRedirectionRegex.Match(metaContent);
        if (!migrationRedirectionMatch.Success)
        {
            migrationRedirectionMatch = migrationRedirectionRegex.Match(htmlText);
        }

        if (migrationRedirectionMatch.Success)
        {
            var redirectResponse = await httpClient.GetAsync(migrationRedirectionMatch.Value);
            if (!redirectResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to follow migration redirection: {redirectResponse.StatusCode}");
            }

            var redirectHtml = await redirectResponse.Content.ReadAsStringAsync();
            document = new HtmlDocument();
            document.LoadHtml(redirectHtml);
        }

        var migrationForm = document.DocumentNode.SelectSingleNode("//form[@name='f']") ?? 
                           document.DocumentNode.SelectSingleNode("//form[@action='https://x.com/x/migrate']");

        if (migrationForm != null)
        {
            var url = migrationForm.GetAttributeValue("action", "https://x.com/x/migrate");
            var method = migrationForm.GetAttributeValue("method", "POST");

            var formData = new List<KeyValuePair<string, string>>();
            var inputFields = migrationForm.SelectNodes(".//input");
            
            if (inputFields != null)
            {
                foreach (var element in inputFields)
                {
                    var name = element.GetAttributeValue("name", "");
                    var value = element.GetAttributeValue("value", "");
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    {
                        formData.Add(new KeyValuePair<string, string>(name, value));
                    }
                }
            }

            var formContent = new FormUrlEncodedContent(formData);
            var formResponse = await httpClient.PostAsync(url, formContent);

            if (!formResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to submit migration form: {formResponse.StatusCode}");
            }

            var formHtml = await formResponse.Content.ReadAsStringAsync();
            document = new HtmlDocument();
            document.LoadHtml(formHtml);
        }

        return document;
    }
}