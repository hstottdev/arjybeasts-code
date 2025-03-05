using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Window Settings", menuName = "ScriptableObjects/Window Settings", order = 1)]
public class WindowSettings : ScriptableObject
{
    [Range(0, 1000)]public float textFileStorageCap;
    [Range(0,1000)]public float imageFileStorageCap;
    [Range(0, 1000)] public float subfolderStorageCap;
    [Range(0,765)]public int colourUnlockTotal;
    public Dictionary<FileType, float> caps;
    public Theme theme;
    public Color windowColour = Color.yellow;
    private void OnValidate()
    {
        SetStorageCap();
    }

    public void SetStorageCap()
    {
        caps = new Dictionary<FileType, float>();
        caps.Add(FileType.text, textFileStorageCap);
        caps.Add(FileType.image, imageFileStorageCap);
        caps.Add(FileType.door, subfolderStorageCap);
    }
}
