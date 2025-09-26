using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace XClientTransactionId;

public class ClientTransaction
{
    private const int AdditionalRandomNumber = 3;
    private const string DefaultKeyword = "obfiowerehiring";
    
    private static readonly Regex OnDemandFileRegex = new(@"(['""])ondemand\.s\1:\s*(['""])([\w]*)\2");
    private static readonly Regex IndicesRegex = new(@"\(\w\[(\d{1,2})\],\s*16\)", RegexOptions.Multiline);

    private int? _defaultRowIndex;
    private int[]? _defaultKeyBytesIndices;
    private string? _key;
    private byte[]? _keyBytes;
    private string? _animationKey;
    private bool _isInitialized;

    private readonly HtmlDocument _homePageDocument;

    public ClientTransaction(HtmlDocument homePageDocument)
    {
        _homePageDocument = homePageDocument;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var indices = await GetIndicesAsync(_homePageDocument);
            _defaultRowIndex = indices.rowIndex;
            _defaultKeyBytesIndices = indices.keyBytesIndices;

            _key = GetKey(_homePageDocument);
            if (string.IsNullOrEmpty(_key))
                throw new Exception("Failed to get key");

            _keyBytes = GetKeyBytes(_key);
            _animationKey = GetAnimationKey(_keyBytes, _homePageDocument);

            _isInitialized = true;
        }
        catch (Exception error)
        {
            Console.WriteLine($"Failed to initialize ClientTransaction: {error}");
            throw;
        }
    }

    public static async Task<ClientTransaction> CreateAsync(HtmlDocument homePageDocument)
    {
        var instance = new ClientTransaction(homePageDocument);
        await instance.InitializeAsync();
        return instance;
    }

    private async Task<(int rowIndex, int[] keyBytesIndices)> GetIndicesAsync(HtmlDocument? homePageDocument = null)
    {
        var keyByteIndicesList = new List<string>();
        var document = homePageDocument ?? _homePageDocument;

        var responseStr = document.DocumentNode.OuterHtml;
        var onDemandFileMatch = OnDemandFileRegex.Match(responseStr);

        if (onDemandFileMatch.Success)
        {
            var onDemandFileUrl = $"https://abs.twimg.com/responsive-web/client-web/ondemand.s.{onDemandFileMatch.Groups[3].Value}a.js";

            try
            {
                using var httpClient = new HttpClient();
                var onDemandFileResponse = await httpClient.GetAsync(onDemandFileUrl);

                if (!onDemandFileResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch ondemand file: {onDemandFileResponse.StatusCode}");
                }

                var responseText = await onDemandFileResponse.Content.ReadAsStringAsync();
                var matches = IndicesRegex.Matches(responseText);

                foreach (Match match in matches)
                {
                    keyByteIndicesList.Add(match.Groups[1].Value);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error fetching ondemand file: {error}");
            }
        }

        if (keyByteIndicesList.Count == 0)
        {
            throw new Exception("Couldn't get KEY_BYTE indices");
        }

        var numericIndices = keyByteIndicesList.Select(int.Parse).ToArray();
        return (numericIndices[0], numericIndices.Skip(1).ToArray());
    }

    private string GetKey(HtmlDocument? response = null)
    {
        response ??= _homePageDocument;

        var element = response.DocumentNode.SelectSingleNode("//meta[@name='twitter-site-verification']");
        var content = element?.GetAttributeValue("content", "") ?? "";

        if (string.IsNullOrEmpty(content))
        {
            throw new Exception("Couldn't get key from the page source");
        }

        return content;
    }

    private byte[] GetKeyBytes(string key)
    {
        return Convert.FromBase64String(key);
    }

    private HtmlNodeCollection GetFrames(HtmlDocument? response = null)
    {
        response ??= _homePageDocument;
        return response.DocumentNode.SelectNodes("//*[starts-with(@id, 'loading-x-anim')]") ?? 
               new HtmlNodeCollection(null);
    }

    private int[][] Get2DArray(byte[] keyBytes, HtmlDocument? response = null, HtmlNodeCollection? frames = null)
    {
        frames ??= GetFrames(response);

        if (frames == null || frames.Count == 0)
        {
            return new int[][] { new int[0] };
        }

        var frame = frames[keyBytes[5] % 4];
        var firstChild = frame.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Element);
        var targetChild = firstChild?.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).Skip(1).FirstOrDefault();
        var dAttr = targetChild?.GetAttributeValue("d", null);

        if (string.IsNullOrEmpty(dAttr))
        {
            return new int[0][];
        }

        var items = dAttr.Substring(9).Split('C');

        return items.Select(item =>
        {
            var cleaned = Regex.Replace(item, @"[^\d]+", " ").Trim();
            if (string.IsNullOrEmpty(cleaned))
                return new int[0];

            var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(int.Parse).ToArray();
        }).ToArray();
    }

    private double Solve(byte value, double minVal, double maxVal, bool rounding)
    {
        var result = (value * (maxVal - minVal)) / 255 + minVal;
        return rounding ? Math.Floor(result) : Math.Round(result * 100) / 100;
    }

    private string Animate(int[] frames, double targetTime)
    {
        var fromColor = frames.Take(3).Select(x => (double)x).Concat(new[] { 1.0 }).ToArray();
        var toColor = frames.Skip(3).Take(3).Select(x => (double)x).Concat(new[] { 1.0 }).ToArray();
        var fromRotation = new[] { 0.0 };
        var toRotation = new[] { Solve((byte)frames[6], 60.0, 360.0, true) };

        var remainingFrames = frames.Skip(7).ToArray();
        var curves = remainingFrames.Select((item, counter) =>
            Solve((byte)item, Utils.IsOdd(counter), 1.0, false)).ToArray();

        var cubic = new Cubic(curves);
        var val = cubic.GetValue(targetTime);
        var color = Interpolation.Interpolate(fromColor, toColor, val)
            .Select(value => Math.Max(value, 0)).ToArray();
        var rotation = Interpolation.Interpolate(fromRotation, toRotation, val);
        var matrix = RotationUtils.ConvertRotationToMatrix(rotation[0]);

        var strList = new List<string>();
        
        foreach (var value in color.Take(color.Length - 1))
        {
            strList.Add(((int)Math.Round(value)).ToString("x"));
        }

        foreach (var value in matrix)
        {
            var rounded = Math.Round(value * 100) / 100;
            if (rounded < 0)
                rounded = -rounded;
            
            var hexValue = Utils.FloatToHex(rounded);
            strList.Add(hexValue.StartsWith(".") ? $"0{hexValue}".ToLower() : hexValue?.ToLower() ?? "0");
        }

        strList.Add("0");
        strList.Add("0");
        
        var animationKey = string.Join("", strList).Replace(".", "").Replace("-", "");
        return animationKey;
    }

    private string GetAnimationKey(byte[] keyBytes, HtmlDocument? response = null)
    {
        const int totalTime = 4096;

        if (_defaultRowIndex == null || _defaultKeyBytesIndices == null)
        {
            throw new Exception("Indices not initialized");
        }

        var rowIndex = keyBytes[_defaultRowIndex.Value] % 16;

        var frameTime = _defaultKeyBytesIndices.Aggregate(1, (num1, num2) => num1 * (keyBytes[num2] % 16));
        frameTime = (int)(Math.Round(frameTime / 10.0) * 10);

        var arr = Get2DArray(keyBytes, response);
        if (arr.Length <= rowIndex || arr[rowIndex] == null)
        {
            throw new Exception("Invalid frame data");
        }

        var frameRow = arr[rowIndex];
        var targetTime = (double)frameTime / totalTime;
        var animationKey = Animate(frameRow, targetTime);

        return animationKey;
    }

    public string GenerateTransactionId(
        string method,
        string path,
        HtmlDocument? response = null,
        string? key = null,
        string? animationKey = null,
        long? timeNow = null)
    {
        if (!_isInitialized)
        {
            throw new Exception("ClientTransaction is not initialized. Call InitializeAsync() before using.");
        }

        timeNow ??= (long)Math.Floor((double)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1682924400));
        var timeNowBytes = new byte[]
        {
            (byte)(timeNow & 0xff),
            (byte)((timeNow >> 8) & 0xff),
            (byte)((timeNow >> 16) & 0xff),
            (byte)((timeNow >> 24) & 0xff)
        };

        key ??= _key ?? GetKey(response);
        var keyBytes = !string.IsNullOrEmpty(key) ? GetKeyBytes(key) : _keyBytes ?? GetKeyBytes(key!);
        animationKey ??= _animationKey ?? GetAnimationKey(keyBytes, response);

        var data = $"{method}!{path}!{timeNow}{DefaultKeyword}{animationKey}";

        byte[] hashBytes;
        using (var sha256 = SHA256.Create())
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var hashBuffer = sha256.ComputeHash(dataBytes);
            hashBytes = hashBuffer.Take(16).ToArray();
        }

        var random = new Random();
        var randomNum = (byte)random.Next(256);
        
        var bytesArr = new List<byte>();
        bytesArr.AddRange(keyBytes);
        bytesArr.AddRange(timeNowBytes);
        bytesArr.AddRange(hashBytes);
        bytesArr.Add(AdditionalRandomNumber);

        var output = new byte[bytesArr.Count + 1];
        output[0] = randomNum;
        
        for (int i = 0; i < bytesArr.Count; i++)
        {
            output[i + 1] = (byte)(bytesArr[i] ^ randomNum);
        }

        return Convert.ToBase64String(output).Replace("=", "");
    }
}