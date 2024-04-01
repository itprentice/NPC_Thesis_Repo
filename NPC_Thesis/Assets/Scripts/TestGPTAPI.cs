using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

//using https://json2csharp.com/ to convert the API response to C# objects
//https://m-ansley.medium.com/unity-web-requests-downloading-and-working-with-json-text-9042b8e001e4
//[System.Serializable]
//public class Usage
//{
//    public int prompt_tokens;
//    public int completion_tokens;
//    public int total_tokens;
//}
//[System.Serializable]
//public class Message
//{
//    public string role;
//    public string content;
//}
//[System.Serializable]
//public class Choice
//{
//    public int index;
//    public Message message;
//    public object logprobs;
//    public string finish_reason;
//}
//[System.Serializable]
//public class GPT_APIResponseRoot
//{
//    public string id;
//    public string @object;
//    public int created;
//    public string model;
//    public List<Choice> choices;
//    public Usage usage;
//    public object system_fingerprint;
//}


public class TestGPTAPI : MonoBehaviour
{
    //public string apiUrl;
    //public string apiKey;
    public GameObject apiHandler;
    public UnityWebRequest currentRequest = null;

    private bool callComplete = false;
    private bool requestSent = false;
    private GPTAPIHandler handler = null;
    // Start is called before the first frame update
    void Start()
    {
        if (apiHandler != null)
        {
            handler = apiHandler.GetComponent<GPTAPIHandler>();
            List<(string role, string content)> messages = new();
            messages.Add(("user", "Hello how are you!"));
            currentRequest = handler.makeRequest(messages);
        }
        else
        {
            callComplete = true; //don't do anything
        }
    }

    void Update()
    {
        if (!callComplete)
        {
            currentRequest.SendWebRequest();
            callComplete = true;
            requestSent = true;
        }
        else if (requestSent && currentRequest.isDone)
        {
            Debug.Log(handler.getResponseMessage(currentRequest));
            requestSent = false;
        }
    }

//    IEnumerator doRequest()
//    {
//        string postData = @"{
//""model"": ""gpt-3.5-turbo"",
//""messages"": [{""role"": ""user"", ""content"": ""Say this is a test""}],
//""temperature"": 0.7
//}";

//        UnityWebRequest req = UnityWebRequest.Post(apiUrl, postData, "application/json");
//        req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

//        yield return req.SendWebRequest();

//        if (req.result != UnityWebRequest.Result.Success)
//        {
//            Debug.Log(req.error);
//        }
//        else
//        {
//            //string responseChoices = req.GetResponseHeader("choices");
//            //Debug.Log(responseChoices);
//            string jsonResponse = req.downloadHandler.text;
//            GPT_APIResponseRoot response = JsonUtility.FromJson<GPT_APIResponseRoot>(jsonResponse);
//            Debug.Log(response.choices[0].message.content);
//        }
    //}
}
