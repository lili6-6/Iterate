using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace pp
{
public class DialogueUI : MonoBehaviour
{
    [Header("Output / Response UI")]
    public RectTransform outputArea;
    public TextMeshProUGUI outputText;

    [Header("Input / Prompt UI")]
    public RectTransform inputArea;
    public UIManagerInputField inputText;

    [Header("Visibility")]
    public CanvasGroup dialogueGroup;
    public bool hideOnStart = true;

    [Header("Default Text")]
    public string waitingText = "NPC 正在思考，请稍候...";
    public string inputHintText = "请输入你的问题，然后点击发送。";
    public string ErrorText = "发生错误，请稍后再试。";

    [Header("Direct Target")]
    public Character_AI characterAI;

    private TMP_InputField _inputField;
    private Selectable[] _inputSelectables;
    private bool _inputLocked;

   

    private void Start()
    {
        if (hideOnStart)
            SetVisible(false);

        // input cache
        if (inputText != null)
        {
            _inputField = inputText.GetComponent<TMP_InputField>()
                       ?? inputText.GetComponentInChildren<TMP_InputField>();
        }

        if (inputArea != null)
        {
            _inputSelectables = inputArea.GetComponentsInChildren<Selectable>(true);
        }

        ApplyInputLockState();
    }

    private void LateUpdate()
    {
        
    }

    // =========================
    // UI FOLLOW LOGIC
    // =========================


    // =========================
    // VISIBILITY
    // =========================
    public void SetVisible(bool visible)
    {
        if (dialogueGroup != null)
        {
            dialogueGroup.alpha = visible ? 1f : 0f;
            dialogueGroup.blocksRaycasts = visible;
            dialogueGroup.interactable = visible;
        }

        gameObject.SetActive(visible);
    }

    // =========================
    // INPUT / OUTPUT
    // =========================
    public void SetInputText(string text)
    {
        if (_inputField != null)
            _inputField.text = text;
    }

    public string GetInputText()
    {
        return _inputField != null ? _inputField.text : string.Empty;
    }

    public void SetOutputText(string text)
    {
        if (outputText != null)
            outputText.text = text;
    }

    public void ShowInputMode()
    {
        if (inputArea != null)
            inputArea.gameObject.SetActive(true);

        if (outputArea != null)
            outputArea.gameObject.SetActive(false);

        if (inputText != null)
        {
            if (inputText.placeholderText != null)
                inputText.placeholderText.text = inputHintText;

            if (_inputField != null && _inputField.placeholder is TMP_Text tmp)
                tmp.text = inputHintText;
        }

        ApplyInputLockState();
    }

    public void ShowReplyMode(string replyText = null)
    {
        if (inputArea != null)
            inputArea.gameObject.SetActive(false);

        if (outputArea != null)
            outputArea.gameObject.SetActive(true);

        if (outputText != null)
            outputText.text = string.IsNullOrEmpty(replyText) ? waitingText : replyText;
    }

    public void SetInputLocked(bool locked)
    {
        _inputLocked = locked;
        ApplyInputLockState();
    }

    // =========================
    // SEND
    // =========================
    public void OnSendButtonClicked()
    {
        SubmitInput();
    }

    public void SubmitInput()
    {
        if (_inputLocked)
            return;

        string prompt = GetInputText();
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        Debug.Log($"[DialogueUI] 输入: {prompt}");

        ShowReplyMode(waitingText);

        if (characterAI != null)
        {
            characterAI.ProcessPlayerPrompt(prompt);
        }
        else
        {
            Debug.LogWarning("characterAI 未绑定");
        }
    }

    // =========================
    // UTIL
    // =========================
    public void ClearOutput() => SetOutputText("");
    public void ClearInput() => SetInputText("");

    public void ResetToInputMode()
    {
        ClearOutput();
        ShowInputMode();
    }

    private void ApplyInputLockState()
    {
        bool interactable = !_inputLocked;

        if (_inputField != null)
        {
            _inputField.interactable = interactable;

            if (!interactable && _inputField.isFocused)
            {
                _inputField.DeactivateInputField();
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _inputField.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }

        if (_inputSelectables != null)
        {
            foreach (var selectable in _inputSelectables)
            {
                if (selectable != null)
                    selectable.interactable = interactable;
            }
        }
    }
}}
