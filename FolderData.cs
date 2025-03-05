using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Folder", menuName = "In-Game File/Folder", order = 1)]
public class FolderData : FileData
{
    [SerializeField] string pathway;

#if UNITY_EDITOR
    public override void OnValidate()
    {
        text = pathway;
        base.OnValidate();
    }
#endif
}
