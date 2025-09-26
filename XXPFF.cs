using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace XClientTransactionId;

public static class XXPFF
{
    public static string GenerateXpForwardedForHeader()
    {
        string hexString = "aaccf4895df25fc39e4867f6ab11f83c24f714383ba5a853abd67da8c2d757a3d82e163d4e868c9827a3a8edb832ad7d492307caae09ff92c8f787a22beb1b0ea01a74b231148c802869ec69026f12607822cd23807af2317bdb0ea67a4cc768e63b04870f88d9976bcbaa95b3ce5b8faf16aa7c498215ea8e7adb5a13c2f3ea747932c1602c77052be197ecb121c2715ae9a3c79175b00e8a7c45e02e963f7a8e46619df08a9af2d537772831b8546f2298690f93147748ba5c0b2338f9e0f823173449f8203e0f619402278fde60b9ae67c3b9cacdb722e36b435b87c9699fa789e5a9176d777c0bbcfa7f171889dc8fcbfffb14f2d38d48ad8d";
        
        return GenerateRandomHexString(
            hexString.Length,  // 512 characters
            includeUppercase: true,
            includeLowercase: true
        ).ToLower();
    }
    
    private static string GenerateRandomHexString(int length, bool includeUppercase = false, bool includeLowercase = false, string[] customChars = null, bool includeNumbers = false, string[] excludeChars = null, bool repeatLowercase = false)
    {
        string result = "";
        List<string> characterPool = new List<string>();
        
        // Add uppercase letters if requested
        if (includeUppercase)
        {
            characterPool.AddRange(GetUppercaseLetters());
        }
        
        // Add lowercase letters if requested
        if (includeLowercase)
        {
            characterPool.AddRange(GetLowercaseLetters());
        }
        
        // Add numbers if requested
        if (includeNumbers)
        {
            characterPool.AddRange(GetNumbers());
        }
        
        // Add custom characters if provided
        if (customChars != null && customChars.Length != 0)
        {
            characterPool.AddRange(customChars);
        }
        
        // Add lowercase letters 30 times if repeatLowercase is true
        if (repeatLowercase)
        {
            for (int i = 0; i < 30; i++)
            {
                characterPool.AddRange(GetLowercaseLetters());
            }
        }
        
        // Remove excluded characters if provided
        if (excludeChars != null && excludeChars.Length != 0)
        {
            characterPool = new List<string>(characterPool.Except(excludeChars));
        }
        
        // Generate random string of specified length
        if (characterPool.Count > 0)
        {
            Random random = new Random();
            for (int j = 0; j < length; j++)
            {
                result += GetRandomCharacterFromPool(characterPool, random);
            }
        }
        
        return result;
    }
     
    private static string[] GetUppercaseLetters()
    {
        return new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    }
     
    private static string[] GetLowercaseLetters()
    {
        return new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
    }
     
    private static string[] GetNumbers()
    {
        return new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    }
     
    private static string GetRandomCharacterFromPool(List<string> characterPool, Random random)
    {
        if (characterPool.Count == 0)
            return "";
        
        int index = random.Next(0, characterPool.Count);
        return characterPool[index];
    }
     
}