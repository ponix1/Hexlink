// SkyboxRotator: menu ambience - continuously rotates the skybox's _Rotation over time.

using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float speed = 0.8f;

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * speed);
    }
}