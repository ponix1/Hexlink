using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float speed = 0.8f;

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * speed);
    }
}