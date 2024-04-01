using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//using https://json2csharp.com/ to convert the API response to C# objects
//https://m-ansley.medium.com/unity-web-requests-downloading-and-working-with-json-text-9042b8e001e4

//class definitions to serialize json responses from the GPT api
[System.Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}
[System.Serializable]
public class Message
{
    public string role;
    public string content;
}
[System.Serializable]
public class Choice
{
    public int index;
    public Message message;
    public object logprobs;
    public string finish_reason;
}
[System.Serializable]
public class GPT_APIResponseRoot
{
    public string id;
    public string @object;
    public int created;
    public string model;
    public List<Choice> choices;
    public Usage usage;
    public object system_fingerprint;
}

/**
 *  handler from the GPT Chat Completions API 
 **/
public class GPTAPIHandler : MonoBehaviour
{
    private const string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey = Config.OPENAI_API_KEY;
    [SerializeField]
    private string model = "gpt-3.5-turbo";

    /**
     * generates a POST request to be sent to chatGPT. Must send web request via
     * req.SendWebRequest() to actually send this request to the GPT API
     **/
    public UnityWebRequest makeRequest(List<(string role, string content)> messages)
    {
        string postData = $@"{{
""model"": ""{model}"",
""messages"": [
";
        foreach (var (role, content) in messages)
        {
            postData += "{" + "\"role\": \"" + role + "\", \"content\": \"" + content + "\"},\n";
        }
        postData = postData.Remove(postData.Length - 2, 1);
        postData += "]}";

        //Debug.Log(postData);
        UnityWebRequest req = UnityWebRequest.Post(apiUrl, postData, "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        return req;
    }
    public UnityWebRequest makeRequest(string role, string content)
    {
        List<(string role, string content)> m = new()
        {
            (role, content)
        };
        return makeRequest(m);
    }

    /*
     * parses a json response from the GPT API and returns the response message text 
     */
    public string getResponseMessage(UnityWebRequest req)
    {
        if (req.isDone)
        {
            string jsonResponse = req.downloadHandler.text;
            GPT_APIResponseRoot response = JsonUtility.FromJson<GPT_APIResponseRoot>(jsonResponse);
            if (response != null && response.choices.Count > 0)
            {
                return response.choices[0].message.content;
            }
            else
            {
                return jsonResponse; //return whole json to inspect error message
            }
           
        }
        else
        {
            return null;
        }
    }

}
