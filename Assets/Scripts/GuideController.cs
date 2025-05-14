using UnityEngine;
using TMPro;
using System.Collections;

public class GuideController : MonoBehaviour
{
    public TextMeshProUGUI hintText;
    private float displayTime = 3f;

    void OnEnable() => ExplorerStateManager.OnStateChanged += ShowHint;
    void OnDisable() => ExplorerStateManager.OnStateChanged -= ShowHint;

    void ShowHint(ExplorerStateManager.ExplorerState state)
    {
        string msg = state switch
        {
            //ExplorerStateManager.ExplorerState.Danger => "Watch out! An enemy is nearby.",
            //ExplorerStateManager.ExplorerState.Walk => "Searching....",
            ExplorerStateManager.ExplorerState.Search => "Keep searching for water.",
            ExplorerStateManager.ExplorerState.Success => "WOW, Water. You’ve found the oasis!",
            ExplorerStateManager.ExplorerState.Idle => "Standing by…",
            _ => null
        };
        if (!string.IsNullOrEmpty(msg))
            StartCoroutine(Display(msg));
    }

    IEnumerator Display(string msg)
    {
        hintText.text = msg;
        hintText.gameObject.SetActive(true);
        yield return new WaitForSeconds(displayTime);
        hintText.gameObject.SetActive(false);
    }
}
