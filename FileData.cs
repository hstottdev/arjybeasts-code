using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Health File", menuName = "In-Game File/Health", order = 1)]
public class FileData : ScriptableObject
{
    public string fileName;
    public int size;
    public FileTypes type;
    public bool editorOnly;
    public int sortingIndex;
    public bool undeletable;
    public bool unignorable;
    public bool showExtension = true;
    public bool singleplayerExitWarning;

    [Header("Opening the file")]
    public bool changeGameModeWhenOpened;
    public GameModes newGameMode;
    public bool loadSceneWhenOpened;
    public string sceneToBeOpened;

    [Header("File Type Overrides")]
    public int stageToGoTo = -1;
    public GameObject overridenOpenWindow;
    public Sprite overridenIcon;

    [HideInInspector]public string text;

    [HideInInspector] public Sprite image;
    

    public virtual int GetSize()
    {
        return size;
    }

#if UNITY_EDITOR
    public virtual void OnValidate()
    {
        size = GetSize();

        //SetAssetNameToFileName();
    }

    void SetAssetNameToFileName()
    {
        string assetPath = AssetDatabase.GetAssetPath(this);
        AssetDatabase.RenameAsset(assetPath, GetAssetName());
    }

    string GetAssetName()
    {
        string assetName;

        int cutOffIndex = 10;

        assetName = type.extension + "_";

        if(fileName.Length > cutOffIndex)
        {
            assetName += fileName.Remove(cutOffIndex);
        }
        else
        {
            assetName += fileName;
        }

        return assetName;
    }
#endif
}
