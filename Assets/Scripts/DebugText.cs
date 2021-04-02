using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DebugText : MonoBehaviour
{
    [SerializeField]
    private TMP_Text infoText;

    public static DebugText Instance { get; private set; }

    private void Awake()
    {
        Debug.Assert(!Instance, $"There's already an Instance of {nameof(DebugText)} assigned, are there multiple in scene?");
        if (Instance) { Destroy(Instance.gameObject); }
        Instance = this;
    }

    public void SetText(string text)
    {
        infoText.text = text;
    }

    public void Append(string text)
    {
        infoText.text += text;
    }

    public void AppendLine(string text) => Append($"{text}\n");
}