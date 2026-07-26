using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HexHitbox : MonoBehaviour
{
    void Start()
    {
        // Ignores clicks on transparent pixels around the hexagon
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}
