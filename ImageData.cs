using SamsStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Arjybeast", menuName = "In-Game File/Image File", order = 1)]
public class ImageData : FileData
{
    [Header("The Boy")]
    [SerializeField]TextAsset arjybeast;

#if UNITY_EDITOR
    public override void OnValidate()
    {
        text = arjybeast.name;
        fileName = Console.stripId(arjybeast.name)[0];
        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), fileName);
        base.OnValidate();
    }
    public override int GetSize()
    {
        var arjybeastExportData = JsonUtility.FromJson<ArjybeastExportData>(arjybeast.text);

        return Console.GetOpponentCost(arjybeastExportData);
    }
#endif
}
