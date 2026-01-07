using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    string _mainScene = "MainScene";
    [SerializeField] float _minDisplayTime = 5f;
    float _textDuration = 4f;
    public CanvasGroup textGroup;

    private IEnumerator Start()
    {
        float startTime = Time.time;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(_mainScene);
        loadOp.allowSceneActivation = false;

        //Fade In
        yield return FadeInCanvasCoroutine(textGroup, 0.5f);

        //Texto
        yield return new WaitForSeconds(_textDuration);

        //Fade out
        yield return FadeOutCanvasCoroutine(textGroup, 0.5f, false);

        while (Time.time - startTime < _minDisplayTime)
        {
            yield return null;
        }

        loadOp.allowSceneActivation = true; //Carga escena
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;
        cg.alpha = start;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        cg.alpha = end;
    }

    private IEnumerator FadeInCanvasCoroutine(CanvasGroup group, float fadeDuration)
    {
        if (group == null) yield break;

        group.alpha = 0f;
        group.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = 1f;
    }

    private IEnumerator FadeOutCanvasCoroutine(CanvasGroup group, float fadeDuration, bool active)
    {
        if (group == null) yield break;

        group.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        group.alpha = 0f;
        Time.timeScale = 1f;
        group.gameObject.SetActive(active);
    }
}

