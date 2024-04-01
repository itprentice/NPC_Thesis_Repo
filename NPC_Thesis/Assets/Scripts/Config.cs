using System.Collections;
using System.IO;
using UnityEngine;

public class Config
{   
    private static readonly string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    public static string OPENAI_API_KEY { get; private set; }

    static Config()
    {
        if (File.Exists(envPath))
        {
            string[] lines = File.ReadAllLines(envPath);

            foreach (string l in lines)
            {
                if (l.StartsWith("OPENAI_API_KEY"))
                {
                    OPENAI_API_KEY = l.Split('=')[1].Trim();
                    Debug.Log("Successfully found OpenAI API key!");
                }
            }
        }
        else
        {
            Debug.LogError(".env file not found. Please create a .env file at "+Directory.GetCurrentDirectory());
        }
    }
}