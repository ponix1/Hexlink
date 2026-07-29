using UnityEngine;
using TMPro;

public class WorldCardUI : MonoBehaviour
{
    public TMP_Text worldNameText;

    public void Setup(WorldData data)
    {
        worldNameText.text = data.worldName;
    }
}