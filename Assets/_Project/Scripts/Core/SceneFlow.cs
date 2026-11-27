using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WG.Core
{
    /// <summary>Переходы между сценами с плавным затемнением.</summary>
    public class SceneFlow : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        public static SceneFlow Instance { get; private set; }
        public static bool IsLoading { get; private set; }

        CanvasGroup fade;

        void Awake()
        {
            Instance = this;
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            fade = canvasGo.GetComponent<CanvasGroup>();
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = Color.black;
            fade.alpha = 1f;
            fade.blocksRaycasts = false;
        }

        public void FadeIn(float duration = 0.8f) => StartCoroutine(FadeTo(0f, duration));

        public static void Load(string scene)
        {
            if (Instance == null) { SceneManager.LoadScene(scene); return; }
            if (!IsLoading) Instance.StartCoroutine(Instance.LoadRoutine(scene));
        }

        IEnumerator LoadRoutine(string scene)
        {
            IsLoading = true;
            fade.blocksRaycasts = true;
            yield return FadeTo(1f, 0.45f);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null; // дать сцене собраться
            IsLoading = false;
            fade.blocksRaycasts = false;
            yield return FadeTo(0f, 0.8f);
        }

        IEnumerator FadeTo(float target, float duration)
        {
            float start = fade.alpha;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / duration));
                yield return null;
            }
            fade.alpha = target;
        }
    }
}
