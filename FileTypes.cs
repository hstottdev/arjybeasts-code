using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FileType
{
    text,
    image,
    folder,
    hlth,
    exe,
    myBeast,
    unlock,
    compressed,
    door,
    tip,
    masterFolder,
    chest
}

[CreateAssetMenu(fileName = "NewType", menuName = "ScriptableObjects/File Type", order = 1)]
public class FileTypes : ScriptableObject
{
    [Tooltip("the name of the file type maaate!")]public string typeName;
    [Tooltip("the file extension, for example '.png'")]public string extension;
    [Tooltip("the sprite to be used for the icon")]public Sprite iconSprite;
    [Tooltip("the window to be instantiated when opening the file")] public GameObject fileOpenWindow;
    [Tooltip("an override for the right click menu")] public GameObject rmbOverride;
    public FileType typeID;
}
