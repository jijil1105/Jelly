using TMPro;
using UnityEngine;

/// <summary>
/// 待機UI
/// </summary>
public class WaitUI : MonoBehaviour
{
    [SerializeField] TMP_Text _countDownText;

    public void Initialized()
    {
        float seconds = (int)(Utility.StartTime - Time.time);
        _countDownText.SetText($"開始まで{seconds}秒");
        SetVisible(true);
    }

    public void OnUpdate()
    {
        int seconds = (int)(Utility.StartTime - Time.time);
        _countDownText.SetText($"開始まで{seconds}秒");
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
