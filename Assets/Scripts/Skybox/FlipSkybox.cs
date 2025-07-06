using UnityEngine;
using System.Collections;

public class FlipSkybox : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private float fadeSpeed = 0.8f;
    [SerializeField] private bool isSkyboxActive = false;
    private Coroutine skyboxFadeCoroutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SkyBoxState(isSkyboxActive);
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.S)) {
            FlipSkyboxState();
        }
    }
    private void SetInitialSkyboxState() {
        if(skyboxMaterial == null) {
            return;
        }

        Color baseColor = skyboxMaterial.GetColor("_BaseColor");
        if(isSkyboxActive) {
            baseColor.a = 1;
        } else {
            baseColor.a = 0;
        }
        skyboxMaterial.SetColor("_BaseColor", baseColor);
    }
    private void FlipSkyboxState() {
        if(skyboxMaterial == null) {
            return;
        }

        isSkyboxActive = !isSkyboxActive;
        if(skyboxFadeCoroutine != null) {
            StopCoroutine(skyboxFadeCoroutine);
        }
        skyboxFadeCoroutine = StartCoroutine(FadeSkybox());
    }

    private IEnumerator FadeSkybox() {
        float targetAlpha = isSkyboxActive ? 1f : 0f;

        Color baseColor = skyboxMaterial.GetColor("_BaseColor");
        float currentAlpha = baseColor.a;

        // Calculate distance to target based on direction
        float distance = isSkyboxActive ? (1f - currentAlpha) : currentAlpha;
        float adjustedFadeSpeed = fadeSpeed * (1f / Mathf.Max(distance, 0.001f)); // avoid divide by zero

        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / adjustedFadeSpeed;
            float alpha = Mathf.Lerp(currentAlpha, targetAlpha, t);

            baseColor.a = alpha;
            skyboxMaterial.SetColor("_BaseColor", baseColor);

            yield return null;
        }

        baseColor.a = targetAlpha;
        skyboxMaterial.SetColor("_BaseColor", baseColor);
    }

    private void SkyBoxState(bool isOn) {
        Color baseColor = skyboxMaterial.GetColor("_BaseColor");
        baseColor.a = isOn ? 1f : 0f;
        skyboxMaterial.SetColor("_BaseColor", baseColor);
    }
}
