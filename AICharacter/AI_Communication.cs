using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using pp;
using PP;

namespace pp
{
public class AI_Communication : MonoBehaviour
{
    [Tooltip("If true, use local fallback response instead of real API.")]
    public bool simulateResponse = false;

    [Tooltip("API Key from Aliyun Bailian.")]
    public string apiKey = "";

    [Tooltip("App ID from Aliyun Bailian.")]
    public string appId = "";
    [SerializeField,Tooltip("SystemMessages"), TextArea(3,20)]
    private string systemMessages = "你是一个NPC，正在和玩家对话。请根据玩家的提问，结合你的背景故事和性格，给出简洁且符合角色设定的回答。";

    [Tooltip("Role name for logging.")]

    private string ApiUrl => $"https://dashscope.aliyuncs.com/api/v1/apps/{appId}/completion";

    public delegate void AIResponseHandler(string response);
    public event AIResponseHandler OnResponseReceived;

    private void Awake()
    {
        // 禁用 SSL 证书验证（仅用于测试，生产环境不建议）
        System.Net.ServicePointManager.ServerCertificateValidationCallback = (cert, key, chain, errors) => true;
    }

    public void RequestResponse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(appId))
        {
            Debug.LogWarning("AI_Communication: apiKey 或 appId 未配置。");
            InvokeSimulatedResponse(prompt);
            return;
        }

        if (simulateResponse)
        {
            InvokeSimulatedResponse(prompt);
            return;
        }

        setPrompt(prompt);
    }
    private void setPrompt(string prompt)
    {
        string fullPrompt=
        "System: " + systemMessages + "\n" +
        "当前角色抵抗度为"+GameManager.Instance.ResistanceDegree+"%" +"\n"+
        "当前角色学习度为"+GameManager.Instance.LearningDegree+"%"+ "\n"+
        "当前周目：5"+ "\n" +
        "角色行为："+"已探索全关卡的80%，已经开始逐渐反抗玩家输入，自主性增强"+"\n"+
        "User输入: " + prompt + "\n" ;

        StartCoroutine(SendRealRequest(fullPrompt));
    }

    private void InvokeSimulatedResponse(string prompt)
    {
        string simulated = "这是一个模拟回复。未来这里可以接入真实 AI 通信。";
        Debug.Log($"[AI_Communication] 模拟回复: {prompt}");
        OnResponseReceived?.Invoke(simulated);
    }

    private IEnumerator SendRealRequest(string prompt)
    {
        Debug.Log($"[AI_Communication] 发起真实请求");
        Debug.Log($"[AI_Communication] URL: {ApiUrl}");
        Debug.Log($"[AI_Communication] AppId: {appId}");
        Debug.Log($"[AI_Communication] Prompt 内容: '{prompt}' (长度: {prompt.Length})");

        string json = BuildJson(prompt);

        Debug.Log($"[AI_Communication] 完整 JSON: {json}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        Debug.Log($"[AI_Communication] JSON 字节数: {bodyRaw.Length}");

        using (var req = new UnityWebRequest(ApiUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            Debug.Log($"[AI_Communication] 请求头已设置");

            yield return req.SendWebRequest();

            Debug.Log($"[AI_Communication] 响应状态码: {req.responseCode}");
            Debug.Log($"[AI_Communication] 响应体长度: {req.downloadHandler.data.Length}");
            Debug.Log($"[AI_Communication] 响应体文本: {req.downloadHandler.text}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AI_Communication] HTTP 请求失败!");
                Debug.LogError($"  结果: {req.result}");
                Debug.LogError($"  状态码: {req.responseCode}");
                Debug.LogError($"  错误: {req.error}");
                OnResponseReceived?.Invoke(this.GetComponent<Character_AI>().dialogueUI.ErrorText);
                yield break;
            }

            try
            {
                var response = JsonUtility.FromJson<BailianAppResponse>(req.downloadHandler.text);
                if (response.output != null && !string.IsNullOrEmpty(response.output.text))
                {
                    Debug.Log($"[AI_Communication] 收到响应: {response.output.text}");
                    OnResponseReceived?.Invoke(response.output.text);
                }
                else
                {
                    Debug.LogError($"[AI_Communication] 响应为空或格式错误");
                    OnResponseReceived?.Invoke(this.GetComponent<Character_AI>().dialogueUI.ErrorText);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AI_Communication] JSON 解析失败: {ex.Message}");
                Debug.LogError($"  原始响应: {req.downloadHandler.text}");
                OnResponseReceived?.Invoke(this.GetComponent<Character_AI>().dialogueUI.ErrorText);
            }
        }
    }

    private string BuildJson(string prompt)
    {
        string escapedPrompt = EscapeJson(prompt);
        return "{\n" +
               "  \"input\": {\n" +
               "    \"prompt\": \"" + escapedPrompt + "\"\n" +
               "  },\n" +
               "  \"parameters\": {\n" +
               "    \"temperature\": 0.7,\n" +
               "    \"top_p\": 0.8,\n" +
               "    \"max_tokens\": 1024\n" +
               "  }\n" +
               "}";
    }

    private string EscapeJson(string input)
    {
        if (input == null)
            return "";
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    // 新版 API 响应体
    [System.Serializable]
    private class BailianAppResponse
    {
        public int status_code;
        public string request_id;
        public string code;
        public string message;
        public OutputData output;
    }

    [System.Serializable]
    private class OutputData
    {
        public string text;
        public string finish_reason;
        public string session_id;
    }
}
}