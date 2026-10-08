// SceneSwitcher: minimal "load this scene" button component. Kept tiny so scene buttons
// can persist a target scene name and fire LoadScene from an onClick listener.

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneToLoad;

    public void LoadScene()
    {
        SceneTransition.To(sceneToLoad);
    }
}