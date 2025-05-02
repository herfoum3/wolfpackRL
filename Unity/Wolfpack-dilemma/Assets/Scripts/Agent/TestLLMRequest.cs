using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TestLLMRequest : MonoBehaviour
{
    private string llmServerUrl = "http://localhost:5000/get_coop_reward";

    private string fixedJson = @"
    {
      ""wolf_histories"": [
        {
          ""id"": 2,
          ""trajectory"": [
            {""x"": ""-0.67"", ""y"": ""2.51"", ""z"": ""0.52""},
            {""x"": ""-0.68"", ""y"": ""2.51"", ""z"": ""0.51""},
            {""x"": ""-0.68"", ""y"": ""2.51"", ""z"": ""0.51""},
            {""x"": ""-0.69"", ""y"": ""2.51"", ""z"": ""0.50""},
            {""x"": ""-0.69"", ""y"": ""2.51"", ""z"": ""0.49""},
            {""x"": ""-0.48"", ""y"": ""2.52"", ""z"": ""0.15""},
            {""x"": ""-0.49"", ""y"": ""2.52"", ""z"": ""0.14""},
            {""x"": ""-0.48"", ""y"": ""2.52"", ""z"": ""0.13""},
            {""x"": ""-0.48"", ""y"": ""2.52"", ""z"": ""0.12""},
            {""x"": ""-0.49"", ""y"": ""2.52"", ""z"": ""0.10""}
          ]
        },
        {
          ""id"": 1,
          ""trajectory"": [
            {""x"": ""0.63"", ""y"": ""2.55"", ""z"": ""-0.80""},
            {""x"": ""0.62"", ""y"": ""2.55"", ""z"": ""-0.79""},
            {""x"": ""0.61"", ""y"": ""2.55"", ""z"": ""-0.78""},
            {""x"": ""0.60"", ""y"": ""2.55"", ""z"": ""-0.76""},
            {""x"": ""0.59"", ""y"": ""2.55"", ""z"": ""-0.74""},
            {""x"": ""0.83"", ""y"": ""2.56"", ""z"": ""-0.62""},
            {""x"": ""0.85"", ""y"": ""2.56"", ""z"": ""-0.63""},
            {""x"": ""0.87"", ""y"": ""2.56"", ""z"": ""-0.64""},
            {""x"": ""0.89"", ""y"": ""2.56"", ""z"": ""-0.64""},
            {""x"": ""0.91"", ""y"": ""2.57"", ""z"": ""-0.65""}
          ]
        }
      ],
      ""deer_history"": [
        {""x"": ""1.60"", ""y"": ""2.61"", ""z"": ""-0.05""},
        {""x"": ""1.59"", ""y"": ""2.61"", ""z"": ""-0.05""},
        {""x"": ""1.58"", ""y"": ""2.60"", ""z"": ""-0.05""},
        {""x"": ""1.58"", ""y"": ""2.61"", ""z"": ""-0.05""},
        {""x"": ""1.58"", ""y"": ""2.61"", ""z"": ""-0.03""},
        {""x"": ""2.01"", ""y"": ""2.63"", ""z"": ""0.30""},
        {""x"": ""2.02"", ""y"": ""2.63"", ""z"": ""0.30""},
        {""x"": ""2.03"", ""y"": ""2.63"", ""z"": ""0.30""},
        {""x"": ""2.04"", ""y"": ""2.63"", ""z"": ""0.29""},
        {""x"": ""2.04"", ""y"": ""2.63"", ""z"": ""0.29""}
      ],
      ""attacker_id"": 1
    }
    ";

    private List<long> responseTimes = new List<long>();

    private void Start()
    {
        StartCoroutine(SendBatchRequests(10));
    }

    private IEnumerator SendBatchRequests(int batchSize)
    {
        for (int i = 0; i < batchSize; i++)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            using (UnityWebRequest request = new UnityWebRequest(llmServerUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(fixedJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                stopwatch.Stop();
                responseTimes.Add(stopwatch.ElapsedMilliseconds);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[LLM Test] Request {i + 1}: {stopwatch.ElapsedMilliseconds}ms | Response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.LogWarning($"[LLM Test] Request {i + 1}: Error {request.error}");
                }
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        float average = 0f;
        if (responseTimes.Count > 0)
        {
            average = (float)responseTimes.Sum() / responseTimes.Count;
        }

        Debug.Log($"[LLM Test] Batch finished. Average response time: {average:F2} ms");
    }
}
