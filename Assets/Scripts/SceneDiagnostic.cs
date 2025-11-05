using UnityEngine;
using UnityEngine.UI;

namespace VRLauncher
{
    /// <summary>
    /// Diagnostic script to check what's in the scene
    /// </summary>
    public class SceneDiagnostic : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("=== SCENE DIAGNOSTIC START ===");

            // Check for Camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Debug.Log($"✓ Main Camera found: {mainCamera.gameObject.name}");
                Debug.Log($"  Position: {mainCamera.transform.position}");
                Debug.Log($"  Rotation: {mainCamera.transform.rotation.eulerAngles}");
                Debug.Log($"  Active: {mainCamera.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogError("✗ No Main Camera found!");
            }

            // Check for TableCarousel
            TableCarousel carousel = FindFirstObjectByType<TableCarousel>();
            if (carousel != null)
            {
                Debug.Log($"✓ TableCarousel found on: {carousel.gameObject.name}");
                Debug.Log($"  Active: {carousel.gameObject.activeInHierarchy}");
                Debug.Log($"  Enabled: {carousel.enabled}");
            }
            else
            {
                Debug.LogError("✗ No TableCarousel found in scene!");
            }

            // Check for Canvas
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"Found {canvases.Length} Canvas(es):");
            foreach (Canvas canvas in canvases)
            {
                Debug.Log($"  Canvas: {canvas.gameObject.name}");
                Debug.Log($"    Position: {canvas.transform.position}");
                Debug.Log($"    Active: {canvas.gameObject.activeInHierarchy}");
                Debug.Log($"    Render Mode: {canvas.renderMode}");
                Debug.Log($"    World Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "NULL")}");
            }

            // Check for Image components
            Image[] images = FindObjectsByType<Image>(FindObjectsSortMode.None);
            Debug.Log($"Found {images.Length} Image component(s):");
            foreach (Image img in images)
            {
                Debug.Log($"  Image on: {img.gameObject.name}, Active: {img.gameObject.activeInHierarchy}");
            }

            // Check for Text components
            Text[] texts = FindObjectsByType<Text>(FindObjectsSortMode.None);
            Debug.Log($"Found {texts.Length} Text component(s):");
            foreach (Text txt in texts)
            {
                Debug.Log($"  Text on: {txt.gameObject.name}, Active: {txt.gameObject.activeInHierarchy}, Content: '{txt.text}'");
            }

            // Check for TextMeshPro components
            TMPro.TextMeshProUGUI[] tmpTexts = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
            Debug.Log($"Found {tmpTexts.Length} TextMeshPro component(s):");
            foreach (var tmp in tmpTexts)
            {
                Debug.Log($"  TMP on: {tmp.gameObject.name}, Active: {tmp.gameObject.activeInHierarchy}, Content: '{tmp.text}'");
            }

            Debug.Log("=== SCENE DIAGNOSTIC END ===");
        }
    }
}
