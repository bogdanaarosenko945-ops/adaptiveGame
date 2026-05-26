using UnityEngine;

public class CardAnimator : MonoBehaviour
{
    public GameObject[] cards;
    public float delayBetween = 0.15f;

    public void AnimateCards()
    {
        foreach (var card in cards)
            card.SetActive(false);

        StartCoroutine(ShowCards());
    }

    System.Collections.IEnumerator ShowCards()
    {
        foreach (var card in cards)
        {
            card.SetActive(true);
            // Анімація масштабу
            card.transform.localScale = Vector3.zero;
            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 5f;
                card.transform.localScale =
                    Vector3.Lerp(
                        Vector3.zero,
                        Vector3.one,
                        t);
                yield return null;
            }
            card.transform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(
                delayBetween);
        }
    }
}