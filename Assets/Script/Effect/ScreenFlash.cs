using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFlash : MonoBehaviour {
    // Call this from anywhere using: ScreenFlash.Flash();
    public static void Flash(Color flashColor, float fadeDuration = 1.0f) {
        GameObject flashObj = new GameObject("ScreenFlashCanvas");
        Canvas canvas = flashObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Draw over absolutely everything

        Image img = flashObj.AddComponent<Image>();
        img.color = flashColor;

        // Attach the fader and let it run
        FlashFader fader = flashObj.AddComponent<FlashFader>();
        fader.StartFade(img, fadeDuration);
    }

    private class FlashFader : MonoBehaviour {
        public void StartFade(Image img, float duration) {
            StartCoroutine(FadeOut(img, duration));
        }

        private IEnumerator FadeOut(Image img, float duration) {
            Color c = img.color;
            float timer = 0f;
            while (timer < duration) {
                timer += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, timer / duration);
                img.color = c;
                yield return null;
            }
            Destroy(gameObject); // Delete the canvas when done!
        }
    }
}