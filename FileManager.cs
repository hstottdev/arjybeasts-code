using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using SamsStuff;
using static GameManager;
using UnityEngine.Events;
using UnityEngine.UI;

public class FileManager : MonoBehaviour
{
    public bool thisPC;
    [SerializeField] float fileGenerationDelay;
    public GameObject rightClickWindow;
    [Header("Location")]
    public bool useDeletionManagerFilePath;
    public string currentFilePath = "Desktop";
    public string visualFilePath;
    public string parentFilePath;
    [SerializeField] GameObject backButton;
    [SerializeField] TextMeshProUGUI filePathwayText;
    [Header("Files")]   
    public List<FileData> filesData;
    public List<FileUI> fileUI;
    public bool randomizeFiles;

    [Header("Settings")]
    public WindowSettings settings;

    [Header("Events")]
    public UnityEvent OnClearedFiles;

    [Header("Debug")]
    public FileData deleteOnLoadedTest;

    //a list for storing which files have been deleted
    public static List<FileData> deletedFiles = new List<FileData>();
    [SerializeField] List<FileData> randomlyGeneratedFiles = new List<FileData>();

    //a dictionary used for storing the randomization of file windows
    public static Dictionary<string,List<FileData>> randomFileData = new Dictionary<string, List<FileData>>();

    public static Dictionary<FileData, Color> randomColourUnlockData = new Dictionary<FileData, Color>();

    public static FileData deleteOnSceneLoaded;


    /// <summary>
    /// <returns><strong>[beastName][id]</strong></returns>
    /// </summary>
    public static List<string> copiedBeastFiles = new List<string>();

    //instance of a right click
    public static RightClickWindow rightClickInstance;

    public virtual void Start()
    {
        if (!thisPC)
        {
            Invoke("UpdateFiles", fileGenerationDelay);
        }
    }

    /// <summary>
    /// <paramref name="minimumDistanceFromEdge"/>: <strong>The minimum distance from the edge of the screen.</strong>
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="minimumDistanceFromEdge">
    /// <strong>the higher the number, the more central it becomes</strong>
    ///  
    /// </param>
    /// 
    /// <returns></returns>
    public static Vector3 GetRandomScreenPoint(Transform canvas, float minimumDistanceFromEdge = 200)
    {        
        RectTransform rect = canvas.GetComponent<RectTransform>();

        float x = Random.Range(rect.rect.xMin + minimumDistanceFromEdge, rect.rect.xMax - minimumDistanceFromEdge);
        float y = Random.Range(rect.rect.yMin + minimumDistanceFromEdge, rect.rect.yMax - minimumDistanceFromEdge);
        float z = 0;

        Vector3 spawnPosition = new Vector3(x, y, z);
        return spawnPosition;
    }
    public void ChangePathway(string newFilePath)
    {
        if (isValidPath(newFilePath))
        {
           currentFilePath = newFilePath;
        }
    }

    public void UpdateFiles()
    {
        filesData = new List<FileData>();//resetting the list

        //debugDeletedFiles = deletedFiles;

        if (useDeletionManagerFilePath && instance != null)
        {
            currentFilePath = DeletionManager.currentFolderPath;
            parentFilePath = DeletionManager.currentParentPath;
        }

        DefineFileUIList();

        if (isValidPath(currentFilePath))
        {
            settings = GetWindowSettings(currentFilePath);

            filesData = GetFilesAtPathway(currentFilePath, randomizeFiles);

            Console.SortFiles(filesData);

            ApplyFilesData();
        }
        else
        {
            Debug.LogWarning("Invalid File Pathway, folder must be in Assets/Game Files/Resources");
        }

        if(backButton != null)
        {
            backButton.SetActive(isValidPath(parentFilePath));
        }


        if (settings != null)
        {
            if(GetComponentInParent<DraggableWindow>() != null)
            {
                GetComponentInParent<DraggableWindow>().GetComponent<Image>().color = settings.windowColour;
            }
        }

        
    }

    public static bool isValidPath(string path)//checks if the given file path is valid
    {
        if(path == "" || path == null)
        {
            return false;
        }

        if (Application.isEditor)
        {
            return Directory.Exists("Assets/Game Files/Resources/" + path);
        }
        else
        {
            return true;
        }
    }

    public static List<FileData> GetSubfolderFiles(string filePath)
    {
        List<FolderData> subfolders = new List<FolderData>();
        subfolders.AddRange(Resources.LoadAll<FolderData>(filePath));

        List<FileData> filesFound = new List<FileData>();//empty list of files found

        foreach(FolderData folder in subfolders)
        {
            filesFound.AddRange(Resources.LoadAll<FileData>(folder.text)); //folder.text is the file pathway of the subfolder
                                                                            //meaning this line will find all the files for a given subfolder
        }
        return filesFound;//return all files found
    }

    public static List<FileData> GetFilesAtPathway(string filePath,bool randomized = true)//retrieves the files generated for a specific file pathway
    {
        List<FileData> allFilesThisFolder = new List<FileData>();
        allFilesThisFolder.AddRange(Resources.LoadAll<FileData>(filePath));
        List<FileData> filesGenerated = new List<FileData>();

       //Debug.Log($"randomFileData length: {randomFileData.Count}");

        if (randomFileData.TryGetValue(filePath, out List<FileData> existingData))//if the files for this window have already been defined
        {
            foreach(FileData f in allFilesThisFolder)
            {
                if (existingData.Contains(f))
                {
                    filesGenerated.Add(f);
                }
            }           
        }
        else//define the random data of the list
        {
            List<FileData> allFiles = new List<FileData>();//create a list containing every file for this window

            allFiles.AddRange(allFilesThisFolder);//retrieve which files need to be loaded 
            //allFiles.AddRange(GetSubfolderFiles(filePath));

            if (randomized)
            {
                Console.Shuffle(allFiles);//shuffle the list
                Debug.Log($"generating new random data for file path: {filePath} ");
            }

            List<FileData> newRandomData = new List<FileData>();

            foreach (FileData f in allFiles)//add files from the shuffled list until the storage cap is reached
            {
                try
                {
                    //Debug.Log($"Checking file:{f.fileName}");
                    float total = GetStorageTotal(newRandomData, f.type.typeID);//get the current total of the files generated of this type
                    float cap = GetStorageCap(f.type.typeID,GetWindowSettings(filePath));

                    bool StorageCapReached = total + f.size >= cap;//if the storage cap is reached
                    bool blockEditorFile = (f.editorOnly && !Application.isEditor);//if its editor only but we aren't in the editor
                    bool blockMissingBeast = (f.type.typeID == FileType.image && Console.GetArjybeastFileToAED(f.text) == null);
                    //bool blockMissingPlayerBeast = (f.type.typeID == FileType.myBeast && beast1 == null);//if I am the player beast file, but beast1 is null

                    //all the above booleans must be false in order for the file to be spawned
                    bool useFile = !StorageCapReached && !blockEditorFile && !blockMissingBeast;

                    //Debug.Log("used up " + GetStorageTotal(filesData, f.type.typeID) + " Bytes of " + f.type.typeName + " out of " + GetStorageCap(f.type.typeID) + " Bytes.");
                    if (useFile)//if not been deleted and storage cap not reached, and not editor only
                    {
                        newRandomData.Add(f);
                        if (allFilesThisFolder.Contains(f))//if this is for my folder
                        {
                            filesGenerated.Add(f);//add it to the list for my folder
                        }                       
                    }
                }
                catch
                {
                    Debug.LogError($"Failed To Load File: {f.fileName}");
                }
            }
            Console.SortFiles(newRandomData);
            Console.SortFiles(filesGenerated);

            //colourList.Sort((a, b) => GetHue(a).CompareTo(GetHue(b)));
            randomFileData.Add(filePath, newRandomData);//add the data to the dictionary       
        }
        filesGenerated = RemoveDeletedFiles(filesGenerated);//remove any files that have been deleted;

        return filesGenerated;
    }

    public static List<FileData> RemoveDeletedFiles(List<FileData> listToRemoveFrom)
    {
        List<FileData> ammendedList = new List<FileData>();//create blank list

        foreach(FileData f in listToRemoveFrom)// for each file
        {
            if (!deletedFiles.Contains(f))//if file has not been deleted
            {
                ammendedList.Add(f);//add it to the list
            }
        }
        return ammendedList;//return the list of non deleted files
    } 

    //gets the storage cap for the file type of a file
    public static float GetStorageCap(FileType fileType, WindowSettings settings)
    {
        float cap = float.MaxValue;
        if(settings != null)
        {
            settings.SetStorageCap();
            if(settings.caps.TryGetValue(fileType, out float _cap))
            {
                cap = _cap;
            }
        }
        //Debug.Log($"{fileType} cap: {cap}");
        return cap;
    }

    public static float GetStorageTotal(List<FileData> fileList,FileType type)
    {
        float total = 0;
        foreach(FileData file in fileList)
        {

            if(file.type.typeID == type)
            {
                //if its a subfolder
                if (type == FileType.door)
                {
                    total += GetTotalStorageOfFolder(file.text);
                }
                else if (file.type.typeID == type)
                {
                    total += file.size;
                }
            }
        }
        return total;
    }

    public static float GetTotalStorageOfFolder(string filePath)
    {
        float folderTotal = 0;
        //Debug.Log($"checking total storage of subfolder: {filePath}");
        List<FileData> files = GetFilesAtPathway(filePath);
        foreach (FileData file in files)
        {
            folderTotal += file.size;
        }

        return folderTotal;
    }

    public static WindowSettings GetWindowSettings(string filePath)
    {
        WindowSettings[] settingsList = Resources.LoadAll<WindowSettings>(filePath);
        if(settingsList.Length > 0)
        {
            return settingsList[0];
        }
        else
        {
            return null;
        }    
    }

    public static bool TryGetCurrentSettings(out WindowSettings settings)
    {
        settings = null;
        if(GameManager.instance != null)
        {
            if (isValidPath(instance.fileWindows[windowInt]))
            {
                settings = GetWindowSettings(instance.fileWindows[windowInt]);
            }
        }
        return settings != null;
    }

    void DefineFileUIList()//setting the variables for UI game objects
    {
        for(int x = 0; x < transform.childCount; x++)
        {
            transform.GetChild(x).gameObject.SetActive(true);//turn on all child objects
        }

        fileUI = new List<FileUI>();
        fileUI.AddRange(GetComponentsInChildren<FileUI>());//add all file UI objects to the list
        List<FileUI> aboutToBeDeleted = new List<FileUI>();

        foreach(FileUI file in fileUI)
        {
            if (file.beenDeleted)
            {
                aboutToBeDeleted.Add(file);//let an already deleted file run it's course...
            }
            else
            {
                file.gameObject.SetActive(false);//turn off all file objects
            }
        }

        //any file ui objects that contain a file thats being deleted should be thrown out
        foreach (FileUI file2 in aboutToBeDeleted)
        {
            //if these objects are used it can cause perfectly healthy files to be destroyed !!!
            if (fileUI.Contains(file2))
            {
                fileUI.Remove(file2);
            }
        }

        

        if(filePathwayText != null)
        {
            if (visualFilePath == "")
            {
                visualFilePath = currentFilePath;//if visual file path is null set it as the current one
            }

            filePathwayText.text = SystemInfo.deviceName + "/" + visualFilePath;//updating the UI for the file pathway

            if(filePathwayText.TryGetComponent(out TextWave t))
            {
                t.originalString = filePathwayText.text;
            }
        } 
    }

    void ApplyFilesData()//applies the file data to the UI game objects
    {

        if(filesData.Count > 0)
        {
            for (int f = 0; f < filesData.Count; f++)//cycle through each file in the folder
            {
                fileUI[f].gameObject.SetActive(true);//for each one turn on a UI file button
                fileUI[f].fileData = filesData[f];// and assign the data to the variable in the UI file script
                fileUI[f].UpdateUI();
                fileUI[f].OnDestroyed.RemoveAllListeners();
            }
        }
    }

    public void FullyClearedCheck()
    {
        int numberOfDeletedFiles = 0;//a variable for number of deleted files

        //for each file...
        foreach(FileData file in filesData)
        {
            bool leftOverHealthFile = file.type.typeID == FileType.hlth && !isValidPath(parentFilePath);
            bool undeletable = file.undeletable || file.type.typeID == FileType.myBeast;

            if (deletedFiles.Contains(file) || leftOverHealthFile || undeletable)//if file has been deleted or is undeletable 
            {
                numberOfDeletedFiles++;//add to the count
            }
        }

        //if all files have been deleted
        if (numberOfDeletedFiles >= filesData.Count)
        {
            //Debug.Log("cleared all files");
            if (isValidPath(parentFilePath))
            {
                BackButton();
            }
            else
            {
                if(GetComponentInParent<DraggableWindow>() != null)
                {
                    GetComponentInParent<DraggableWindow>().Close();
                }
                if (DeletionManager.inst != null && currentFilePath == instance.fileWindows[windowInt])
                {
                    DeletionManager.inst.onFinishedWindow.Invoke();
                }
            }
        }
    }
    
    public static void ClearCopiedBeasts()
    {
        foreach(string s in copiedBeastFiles)
        {
            Console.DeleteBeastFile(s);
        }

        copiedBeastFiles.Clear();
    }

    public void BackButton()
    {
        if (isValidPath(parentFilePath))
        {
            DeletionManager.ChangeFolder(this, parentFilePath);
        }
    }
}
