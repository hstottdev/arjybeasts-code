using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static FileManager;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class FileUI : MonoBehaviour
{
    bool selected;
    bool markedForDeletion;
    bool hoveringOver;
    float timeSinceSelected;
    bool beenOpened;
    [SerializeField] bool deleteable = true;

    [Header("Window Opening")]
    [SerializeField] float doubleClickTimeFrame;
    [SerializeField] bool openWithSingleClick;
    public GameObject openedWindow;

    public static List<GameObject> allOpenedWindows = new List<GameObject>();

    GameObject openedErrorWindow;

    [Header("File Data")]
    public FileData fileData;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI fileNameText; 
    [SerializeField] TextMeshProUGUI fileSizeText;
    [SerializeField] TextMeshProUGUI fileTypeUIText;
    public Image iconImage;
    public RawImage iconRawImage;
    public ArjybeastSpawner spwner;

    FileManager fileManager;
    Transform canvasParent;
    public bool hasBeenOpened;
    public static FileUI playerBeastFile;
    [HideInInspector] public UnityEvent OnDestroyed;
    [HideInInspector] public bool beenDeleted;
    //public GameObject openButton;
    public GameObject deleteButton;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            fileManager = GetComponentInParent<FileManager>();
        }
        catch
        {
            Debug.LogError("No file manager some god damn how");
        }
        try
        {
            canvasParent = GameManager.GetWindowCanvas();
        }
        catch
        {
            canvasParent = GetComponentInParent<Canvas>().transform;
        }

        UpdateUI();
        Invoke("TryDeleteOnLoaded",0.1f);
        
    }

    void TryDeleteOnLoaded()
    {
        if (deleteOnSceneLoaded == null && fileManager.deleteOnLoadedTest != null)
        {
            deleteOnSceneLoaded = fileManager.deleteOnLoadedTest;
        }
        //Debug.Log($"checking whether to delete {fileData.fileName}");
        if (deleteOnSceneLoaded == fileData)
        {
            Debug.Log($"[desktop bug] attempting to delete {fileData.fileName}");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Invoke("DeleteOnSceneLoad", 1);
        }
    }

    void DeleteOnSceneLoad()
    {
        Delete(false);     
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        deleteOnSceneLoaded = null;
    }

    public void UpdateUI()
    {
        if (fileData.type.typeID == FileType.masterFolder)
        {
            Debug.Log("window " + GameManager.windowInt + 1 + "   stage to go to " + fileData.stageToGoTo);
            if (GameManager.windowInt + 1 == fileData.stageToGoTo)
            {
                GetComponent<EventTrigger>().enabled = true;
                GetComponent<CanvasGroup>().alpha = 1.0f;
            }
            else
            {
                GetComponent<EventTrigger>().enabled = false;
                GetComponent<CanvasGroup>().alpha = 0.3f;
            }
        }
        if (fileData.type.typeID == FileType.myBeast)
        {
            //Debug.LogError("seeting playerBeastUIObject what the FUUUUCK");
            if (GameManager.beast1 == null)
            {
                
                DeletionManager.playerBeastUIObject = gameObject;
                gameObject.SetActive(false);
                return;
            }
            else
            {
                playerBeastFile = this;
            }
        }


        fileNameText.text = GetFileName();

        if (fileData.showExtension)
        {
            fileNameText.text += fileData.type.extension;
        }

        SetFileIcon();

        if(fileTypeUIText != null)
        {
            fileTypeUIText.text = fileData.type.typeName;
        }

        if(fileSizeText != null)
        {
            fileSizeText.text = "";
            float size;

            if(fileData.type.typeID == FileType.door)
            {
                size = GetTotalStorageOfFolder(fileData.text);

                Debug.Log($"total storage of {fileData.text} is : {size}");
            }
            else
            {
                size = fileData.size;
            }

            if (size > 0)
            {
                fileSizeText.text = size.ToString() + " Bytes";
            }
        }

/*        if(fileData.type.typeID == FileType.text)
        {
            openWithSingleClick = true;
        }*/
    }

    string GetFileName()
    {
        if (fileData.type.typeID == FileType.myBeast)
        {
            return SamsStuff.Console.stripId(GameManager.beast1)[0];
        }
        return fileData.fileName;
    }

    void SetFileIcon()
    {
        bool isPlayerBeast = fileData.type.typeID == FileType.myBeast;
        bool isEnemyBeast = fileData.type.typeID == FileType.image;

        if (isPlayerBeast)//if its the player arjybeast
        {
            spwner.gameObject.SetActive(true);
        }
        else if (isEnemyBeast)
        {
            iconRawImage.texture = SamsStuff.Console.GetArjybeastFileToTexture2D(fileData.text, true);
        }
        else
        {
            iconImage.sprite = GetFileIcon();
        }

        spwner.gameObject.SetActive(isPlayerBeast);
        iconRawImage.enabled = isEnemyBeast;
        iconImage.enabled = !isPlayerBeast && !isEnemyBeast;

        //setting the random colour unlock data
        if (fileData.type.typeID == FileType.unlock)
        {
            if (randomColourUnlockData.ContainsKey(fileData))
            {
                iconImage.color = randomColourUnlockData[fileData];
            }
            else if(TryGetCurrentSettings(out WindowSettings settings))
            {
                iconImage.color = UnlockColour.GenerateColour(settings.colourUnlockTotal);//store the color that will be unlocked in the icon colour
                randomColourUnlockData.Add(fileData, iconImage.color);
            }
        }
    }

    Sprite GetFileIcon()
    {
        Sprite icon;
        if (fileData.overridenIcon != null)
        {
            icon = fileData.overridenIcon;
        }
        else
        {
            icon = fileData.type.iconSprite;
        }
        return icon;
    }
    public IEnumerator deleteAfterClosingWindow()
    {
        markedForDeletion = true;
        yield return new WaitUntil(() => openedWindow == null);
        Delete();
        markedForDeletion = false;
    }
    private void Update()
    {
        if (fileData.type.typeID == FileType.chest)
        {
            chestFile cf = (chestFile)fileData;
            if (openedWindow != null)
            {
                if (openedWindow.GetComponent<chest>().unlocked)
                {
                    if (!markedForDeletion)
                    {
                        cf.undeletable = false;
                        StartCoroutine(deleteAfterClosingWindow());
                    }
                }
            }
        }
        else
        {
            if (openedWindow != null)
            {
                if (!markedForDeletion)
                {
                    StartCoroutine(deleteAfterClosingWindow());
                }
            }
        }

        DeleteCheck();

        if (selected)
        {
            //GetComponent<Button>().Select();
            //if (openButton != null)
            //{
            //    openButton.SetActive(true);
            //}
            if (deleteButton != null)
            {
                deleteButton.SetActive(deleteable);
            }
            timeSinceSelected += Time.deltaTime;
        }
        else
        {
            if (deleteButton != null)
            {
                deleteButton.SetActive(false);
            }
        }

        RightClickCheck();
    }

    bool interactableCheck()
    {
        bool interactable = true;
        if (fileManager.GetComponent<CanvasGroup>() != null)
        {
            interactable = fileManager.GetComponent<CanvasGroup>().interactable;
        }
        return interactable;
    }

    void RightClickCheck()
    {
        bool windowAlreadyExists = rightClickInstance != null;

        bool rightClickPrefabExists = false;
        if (fileManager != null)
        {
            rightClickPrefabExists = fileManager.rightClickWindow != null;
        }

        //opening the right click window
        if (hoveringOver && Input.GetMouseButtonDown(1) && rightClickPrefabExists && interactableCheck())
        {
            if (windowAlreadyExists)
            {
                Destroy(rightClickInstance.gameObject);
            }

            GameObject rightClickWin = Instantiate(GetRightClickPrefab(), persistentCanvas.mousePosition, transform.rotation, canvasParent);

            rightClickInstance = rightClickWin.GetComponent<RightClickWindow>();

            rightClickInstance.file = this;

            AudioManager.PlaySound("click1", 0.7f,0.2f);
        }

        //removing an existing right click window
        if(windowAlreadyExists)
        {
            bool meWasRightClickedOn = rightClickInstance.file == this;//if it was me that was right clicked on

            bool mouseOver = hoveringOver || rightClickInstance.hoveringOver;//if either hovering over the file or the right click window itself

            bool windowCloseInput = Input.GetMouseButtonDown(0);//if the player presses the button to close


            //if this that and the other
            if (meWasRightClickedOn && !mouseOver && windowCloseInput)
            {
                rightClickInstance.Close();//close me
            }
        }
    }

    GameObject GetRightClickPrefab()
    {
        if(fileData.type.rmbOverride != null)
        {
            return fileData.type.rmbOverride;//use override
        }
        else
        {
            return fileManager.rightClickWindow;//use default
        }
    }

    public void Hover()
    {
        hoveringOver = true;
    }

    public void UnHover()
    {
        hoveringOver = false;
    }

    void DeleteCheck()
    {
        //if (Input.GetKeyDown("delete") && selected)
        //{
        //    if(fileData.type.typeID == FileType.hlth || fileData.type.typeID == FileType.unlock)
        //    {
        //        Open();
        //    }
        //    else
        //    {
        //        Delete();
        //    }
        //}
    }


    private void OnDestroy()
    {
        //Debug.Log($"[desktop bug] destroying object for {fileData.fileName}");
        OnDestroyed.Invoke();
    }

    //declare a file as deleted globally
    void DeclareStaticDeletion()
    {
        deletedFiles.Add(fileData);
        GameManager.Save();
        fileManager.Invoke("FullyClearedCheck", 0.5f);
    }

    public void Delete(bool rewardStorage = true)
    {

        //close right click instance if it exists
        if (rightClickInstance != null)
        {
            rightClickInstance.Close();
        }



        if (CanDelete())
        {
            //Debug.Log($"[desktop bug] deleting {fileData.fileName}");

            CloseOpenWindow(openedWindow);

            if (fileData.type.typeID == FileType.image && GameManager.beast2 != fileData.text)
            {
                FMODController.instance.PlayEvent("SFX/TriedToDelete");
                GameManager.beast2 = fileData.text;//setting the enemy as this file

                deleteOnSceneLoaded = fileData;//this makes it so the file gets deleted once the battle is over

                transition.BattleTransition(fileData.fileName, canvasParent);//transition to battle scene
            }
            else
            {
                //Debug.Log("Deleting " + fileData.fileName);

                FMODController.instance.PlayEvent("SFX/Deleted");
                colourFade.FadeObject(gameObject);//this makes the game object fade out an destroy after a second or 2
                beenDeleted = true;

                DeclareStaticDeletion();


                if (rewardStorage)
                {
                    GameManager.storageSpaceLeft += fileData.size;
                }

                //AudioManager.PlaySound("click1", Random.Range(0.1f, 0.3f));
            }
        }


    }

    bool CanDelete()
    {
        //if not deleteable under any circumstances...
        if (!deleteable)//then skip the whole function lol
        {
            return false;
        }

        string error = "Deletion Error";

        CloseOpenWindow(openedErrorWindow);

        if (GameManager.beast1 == null && GameManager.instance.currentMode == GameModes.SINGLEPLAYER && fileData.type.typeID != FileType.unlock)
        {
            string errorReason = "You can't delete files without an <b>ARJYBEAST</b>.";
            openedErrorWindow = ErrorWindow.Spawn(error, errorReason, canvasParent).gameObject;
            return false;
        }

        if (fileData.type.typeID == FileType.tip)
        {
            if (!hasBeenOpened)
            {
                string errorReason = "You must open a tip file before deleting.\n<b>For your own good</b>";
                openedErrorWindow = ErrorWindow.Spawn(error, errorReason, canvasParent).gameObject;
                return false;
            }
        }

        //this bit checks if a subfolder is empty before deleting it
        if (fileData.type.typeID == FileType.folder || fileData.type.typeID == FileType.door)
        {
            bool unDeletedFileExists = false;

            foreach (FileData file in GetFilesAtPathway(fileData.text))
            {
                if (!deletedFiles.Contains(file) && file.type.typeID != FileType.hlth)
                {
                    unDeletedFileExists = true;
                }
            }

            if (unDeletedFileExists)
            {
                string errorReason = $"You must delete each file inside '<b>{GetFileName()}</b>'!";
                openedErrorWindow = ErrorWindow.Spawn(error, errorReason,canvasParent).gameObject;
                openedErrorWindow.GetComponent<DraggableWindow>().OnClose.AddListener(Open);
                return false;
            }
        }
        if (fileData.type.typeID == FileType.myBeast || fileData.undeletable)
        {
            string errorReason = "I need to keep <b>'" + GetFileName()+"</b>'!";
            openedErrorWindow = ErrorWindow.Spawn(error, errorReason, canvasParent).gameObject;
            return false;
        }


        return true;
    }

    public void Select()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //AudioManager.PlaySound("click1", 1.8f);
            foreach(FileUI file in fileManager.fileUI)
            {
                if(file != this)
                {
                    file.Deselect();
                }
            }

            bool willOpen = (selected && timeSinceSelected < doubleClickTimeFrame) || openWithSingleClick;

            if (willOpen)
            {
                Open();
            }
            selected = true;
            timeSinceSelected = 0;
        }
    }

    public void Deselect()
    {
        selected = false;
        Debug.Log("DESELECTED");
    }

    public void Open()
    {
        //close right click instance if it exists
        if (rightClickInstance != null)
        {
            rightClickInstance.Close();
        }

        if (!markedForDeletion)
        {
            //single player exit warning
            if (fileData.singleplayerExitWarning && DeletionManager.sceneState != DeletionManager.SceneState.Desktop)
            {
                ResetWindow rWin = ResetWindow.Spawn(canvasParent);

                rWin.continueButton.onClick.AddListener(ResetForExit);
                rWin.continueButton.onClick.AddListener(SceneLoadCheck);
                rWin.continueButton.onClick.AddListener(OpenWindowCheck);
                rWin.continueButton.onClick.AddListener(ChangeModeCheck);

                void ResetForExit()
                {
                    GameManager.ResetForDesktop(true);
                }

                return;
            }

            ChangeModeCheck();

            SceneLoadCheck();

            OpenWindowCheck();

            if (fileData.type.typeID == FileType.hlth)
            {
                CloseOpenWindow(openedErrorWindow);

                if (GameManager.beast1 == null)
                {
                    openedErrorWindow = ErrorWindow.Spawn("Heal Error", "You have no <b>ARJYBEAST!</b> <color=#303030>(Numpty)", canvasParent).gameObject;
                }
                else if (GameManager.beast1Health == GameManager.beast1MaxHealth) // no damidge
                {
                    openedErrorWindow = ErrorWindow.Spawn("Heal Error", "You have <b>FULL HEALTH!</b> <color=grey>(Numpty)", canvasParent).gameObject;
                }
                else
                {
                    ArjybeastHealer healer = ArjybeastHealer.Spawn(GameManager.beast1, fileData.size, canvasParent, 0.5f);
                    healer.onHealed.AddListener(healer.window.Close);
                    Delete(false);
                    fileManager.Invoke("UpdateFiles", 1);
                }
            }

            if (fileData.type.typeID == FileType.unlock)
            {
                UnlockColour unlockWin = openedWindow.GetComponentInChildren<UnlockColour>();

                if (randomColourUnlockData.ContainsKey(fileData))
                {
                    unlockWin.lockedColour.color = randomColourUnlockData[fileData];
                }
                else if (TryGetCurrentSettings(out WindowSettings settings))
                {
                    unlockWin.lockedColour.color = UnlockColour.GenerateColour(settings.colourUnlockTotal);//store the color that will be unlocked in the icon colour
                    randomColourUnlockData.Add(fileData, unlockWin.lockedColour.color);
                }


                unlockWin.onUnlocked.AddListener(DeleteColourListener);
                void DeleteColourListener()
                {
                    Delete(false);
                }
            }

            if (fileData.type.typeID == FileType.image)
            {
                OpenNewWindow(DeletionManager.inst.enemyPropertiesPrefab);
            }

            if (fileData.type.typeID == FileType.door && fileManager.enabled)
            {
                DeletionManager.ChangeFolder(fileManager, fileData.text);
            }

            if (fileData.type.typeID == FileType.masterFolder)
            {
                GameManager.windowInt = fileData.stageToGoTo;
                DeletionManager.sceneState = DeletionManager.SceneState.Deleting;
                DeletionManager.currentFolderPath = GameManager.instance.fileWindows[GameManager.windowInt];
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        else
        {
            openedErrorWindow = ErrorWindow.Spawn("cannot open file", "this file is marked for deletion", canvasParent).gameObject;
        }
    }

    void SceneLoadCheck()
    {
        if (!beenOpened)
        {
            beenOpened = true;
            if (fileData.loadSceneWhenOpened)
            {
                if (fileData.sceneToBeOpened == "Pant")
                {
                    ButtonManager.OpenPant(GameManager.GetPlayerBeast());
                }
                else
                {
                    ButtonManager.LoadScene(fileData.sceneToBeOpened);
                    try
                    {
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogWarning("invalid scene path for file:" + fileData.fileName);
                    }
                }
            }
        }
    }

    void OpenWindowCheck()
    {
        GameObject windowToOpen;
        //if the opened window is being overridden
        if (fileData.overridenOpenWindow != null)
        {
            windowToOpen = fileData.overridenOpenWindow;
        }
        else//otherwise use the normal window to be opened for that file type
        {
            windowToOpen = fileData.type.fileOpenWindow;
        }

        //files that open a window are opened in here
        if (windowToOpen != null)
        {
            OpenNewWindow(windowToOpen);
        }
    }

    void ChangeModeCheck()
    {
        //changing mode if the file wants to do that
        if (fileData.changeGameModeWhenOpened)
        {
            GameManager.instance.currentMode = fileData.newGameMode;
        }
    }

    void OpenNewWindow(GameObject window)
    {
        //bool windowPreviouslyExisted = openedWindow != null;
        hasBeenOpened = true;
        Vector3 windowSpawnPosition;

        try
        {
            windowSpawnPosition = GetWindowRect().localPosition;
            CloseOpenWindow(openedWindow);
        }
        catch
        {
            windowSpawnPosition = GetRandomScreenPoint(canvasParent);
        }

        //spawning the window
        openedWindow = Instantiate(window,canvasParent);
        if (fileData.type.typeID == FileType.chest)
        {
            chestFile cf = (chestFile)fileData;
            openedWindow.GetComponent<chest>().Rewards = cf.rewards;
        }
        allOpenedWindows.Add(openedWindow);

        //Getting the rect transform of the new window
        RectTransform windoRect = GetWindowRect();

        //setting the position to the random spawn position
        windoRect.localPosition = windowSpawnPosition;

        AssignWindowVariables(openedWindow);
    }

    public static void CloseOpenWindow(GameObject window, bool fade = true)
    {
        if(window != null)
        {
            Debug.Log("window " + window.name);
            if (allOpenedWindows.Contains(window))
            {
                allOpenedWindows.Remove(window);
            }
            if (window.TryGetComponent(out DraggableWindow dw) && fade)
            {
                dw.OnClose.RemoveAllListeners();
                dw.Close();
            }
            else
            {
                Destroy(window);
            }
        }
        else
        {
            allOpenedWindows.Remove(window);
        }
    }

    public static void CloseAllWindows()
    {
        int x = allOpenedWindows.Count;
        for (int i = 0; i < x; i++)
        {
            CloseOpenWindow(allOpenedWindows[0]);
        }
    }

    RectTransform GetWindowRect()
    {
        return openedWindow.GetComponentInChildren<ScrollRect>().content;
    }

    void AssignWindowVariables(GameObject window)
    {
        //assigning data to the windows relating to the file that was opened
        if (fileData.type.typeID == FileType.text || fileData.type.typeID == FileType.tip)
        {
            window.GetComponent<Notpad>().textData = fileData;//setting the data up for the text file window
        }
        if (fileData.type.typeID == FileType.folder)
        {
            FileManager newFileManager = window.GetComponentInChildren<FileManager>();
            newFileManager.ChangePathway(fileData.text);
            newFileManager.visualFilePath = fileManager.visualFilePath + "/" + fileData.fileName;
            newFileManager.UpdateFiles();

        }
        if(fileData.type.typeID == FileType.image)
        {
            ArjybeastPropertyWindow properties = window.GetComponentInChildren<ArjybeastPropertyWindow>();
            properties.beastToLoad = Beasts.Custom;
            properties.beastToBeLoaded = fileData.text;
            properties.SpawnBeast();
        }
    }

    private void OnDisable()
    {
        CloseOpenWindow(openedWindow);
        CloseOpenWindow(openedErrorWindow);
    }

    public static GameObject getIfAlreadyOpen(string windowName)
    {
        foreach (GameObject window in allOpenedWindows)
        {
            if (window.GetComponent<DraggableWindow>().windowName == windowName)
            {
                return window;
            }
        }
        return null;
    }
}
