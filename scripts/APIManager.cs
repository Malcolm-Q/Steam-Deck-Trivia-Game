using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Web;

[Serializable]
public class TriviaData
{
    public int response_code;
    public List<TriviaQuestion> results;
}

[Serializable]
public class TriviaQuestion
{
    public string category;
    public string type;
    public string difficulty;
    public string question;
    public string correct_answer;
    public string[] incorrect_answers;
}


public class APIManager : MonoBehaviour
{
    public static APIManager instance;
    public void Start()
    {
        instance = this;
    }

    public static IEnumerator GetDataFromApi(string url,System.Action<TriviaData> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            callback(null);
        }
        else
        {
            // Parse the data received from the API
            string data = www.downloadHandler.text;
            TriviaData triviaData = JsonUtility.FromJson<TriviaData>(data);
            callback(triviaData);
        }
    }
}
