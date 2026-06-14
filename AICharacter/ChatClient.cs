using System;
using System.Text;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

namespace Halabang.Blueberry.pp
{
    public class ChatClient : MonoBehaviour
    {
        [Header("百炼配置")]
        [SerializeField] private string apiKey = "sk-xxx";
        [SerializeField] private string appId = "your_app_id";

        private string ApiUrl =>
            $"https://dashscope.aliyuncs.com/api/v1/apps/{appId}/completion";

        public void SetApiKey(string key) => apiKey = key;
        public void SetAppId(string id) => appId = id;

        // =========================
        // 对外调用
        // =========================
        public IEnumerator SendPrompt(
            string role,
            string prompt,
            Action<string> onSuccess,
            Action<string> onError = null
        )
        {
            if (string.IsNullOrEmpty(prompt))
            {
                onError?.Invoke("prompt 不能为空");
                yield break;
            }

            var sw = Stopwatch.StartNew();

            // =========================
            // 构造 JSON（关键：绝对不会丢 prompt）
            // =========================
            string json = BuildJson(prompt);

            UnityEngine.Debug.Log("===== REQUEST JSON =====");
            UnityEngine.Debug.Log(json);

            using var req = new UnityWebRequest(ApiUrl, "POST");

            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return req.SendWebRequest();

            // =========================
            // HTTP错误
            // =========================
            if (req.result != UnityWebRequest.Result.Success)
            {
                string err = BuildError(req, json);

                UnityEngine.Debug.LogError(err);
                onError?.Invoke(err);
                yield break;
            }

            string raw = req.downloadHandler.text;

            UnityEngine.Debug.Log("===== RESPONSE =====");
            UnityEngine.Debug.Log(raw);

            // =========================
            // 解析返回（安全方式）
            // =========================
            BailianResponse res = JsonUtility.FromJson<BailianResponse>(raw);

            string text = res?.output?.text;

            if (string.IsNullOrEmpty(text))
            {
                string err = "返回为空或解析失败:\n" + raw;
                UnityEngine.Debug.LogError(err);
                onError?.Invoke(err);
                yield break;
            }

            onSuccess?.Invoke(text);

            sw.Stop();
            UnityEngine.Debug.Log($"[{role}] 耗时: {sw.ElapsedMilliseconds} ms");
        }

        // =========================
        // JSON 构造（核心防错点）
        // =========================
        private string BuildJson(string prompt)
        {
            string safePrompt = Escape(prompt);

            return
$@"{{
  ""input"": {{
    ""prompt"": ""{safePrompt}""
  }},
  ""parameters"": {{
    ""temperature"": 0.7,
    ""top_p"": 0.8,
    ""max_tokens"": 1024
  }}
}}";
        }

        // =========================
        // JSON安全转义
        // =========================
        private string Escape(string s)
        {
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
        }

        // =========================
        // 错误信息增强（非常重要）
        // =========================
        private string BuildError(UnityWebRequest req, string json)
        {
            return
$@"===== AI REQUEST FAILED =====
HTTP Code: {req.responseCode}
Error: {req.error}

Response Body:
{req.downloadHandler.text}

Request JSON:
{json}";
        }

        // =========================
        // Response DTO
        // =========================
        [Serializable]
        private class BailianResponse
        {
            public Output output;
            public string request_id;
        }

        [Serializable]
        private class Output
        {
            public string text;
            public string finish_reason;
        }
    }
}