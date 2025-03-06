using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SamsStuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CanvasGroup))]
public class DraggableWindow : MonoBehaviour
{
        //Editor Utility for Spawning Draggable Windows
        public string windowName;
        #if UNITY_EDITOR
        [MenuItem("GameObject/Draggable Window",false,8)]
        static void SpawnMe()
        {
            PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Draggable Simpler"),Selection.activeTransform);          
        }
        #endif

    [HideInInspector] public RectTransform actualWindow;
    [SerializeField] float offscreenLerpSpeed = 4;
    [SerializeField] GameObject flash;

    [TextArea]
    [SerializeField] string note;

    bool horizontallyOffscreen;
    bool verticallyOffscreen;
    bool beingDragged;

    ScrollRect sRect;
    Canvas canvas;
    float originalSensitivity;

    [SerializeField] bool moveToWindowCanvasOnStart;

    [Space]
    [SerializeField][Tooltip("Tick this if the window should go transparent if obscurers are in front of it.")] bool obscuree;
    [SerializeField][Tooltip("Tick this to make obscurees behind it go transparent.")] bool obscurer;

    public UnityEvent OnClose;

    [SerializeField] ScaleState scaleState;
    [SerializeField] float scaleProgress;
    [SerializeField] float scaleTime;
    [SerializeField] [Range(0, 1)] float squeezeAmount;
    Vector3 initialScale;
    bool readyToScale;
    private void Start()
    {
        sRect = GetComponentInChildren<ScrollRect>();
        actualWindow = sRect.content;

        SetCanvas();
        FixSiblingRelationship();
        originalSensitivity = sRect.scrollSensitivity;

        scaleProgress = 0f;
        readyToScale = true;
        initialScale = transform.localScale;

        FMODController.instance.PlayEvent("SFX/Open Window");
        
        ScaleIn();
        PerformScaleIteration();
    }

    void ScaleIn()
    {
        scaleState = ScaleState.In;
    }

    void ScaleOut()
    {
        scaleState = ScaleState.Out;
    }

    void SetCanvas()
    {
        Transform windowCanvas = GameManager.GetWindowCanvas();

        if (moveToWindowCanvasOnStart && windowCanvas != null)
        {
            transform.SetParent(windowCanvas);

            MoveToFront();

            if (!FileUI.allOpenedWindows.Contains(gameObject))//if doesn't contain
            {
                FileUI.allOpenedWindows.Add(gameObject);//add
            }
        }

        canvas = GetComponentInParent<Canvas>();
        sRect.viewport = canvas.GetComponent<RectTransform>();
    }

    void FixSiblingRelationship()
    {
        sRect.transform.SetAsFirstSibling();

        if(flash != null)
        {
            flash.transform.SetAsLastSibling();
        }
    }

    enum ScaleState
    { 
        In,
        Out
    }


    private void Update()
    {
        OffscreenCheck();

        if (obscuree)
        {
            SetAlpha();
        }

        MouseWheelCancel();

        if (!beingDragged)
        {
            if (horizontallyOffscreen)
            {
                //Horizontal Offscreen
                actualWindow.localPosition = new Vector2(Mathf.Lerp(actualWindow.localPosition.x, 0, Time.deltaTime * offscreenLerpSpeed), actualWindow.localPosition.y);
            }
            if (verticallyOffscreen)
            {
                //Vertical Offscreen
                actualWindow.localPosition = new Vector2(actualWindow.localPosition.x, Mathf.Lerp(actualWindow.localPosition.y, 0, Time.deltaTime * offscreenLerpSpeed));
            }
        }
        PerformScaleIteration();
    }

    public void PerformScaleIteration()
    {

        if (readyToScale)
        {
            switch (scaleState)
            {
                case ScaleState.In:
                    transform.localScale = Vector3.Lerp(initialScale * squeezeAmount, initialScale, Console.Ease(scaleProgress, Console.EaseMode.EaseOut));
                    scaleProgress += Time.deltaTime / scaleTime;
                    if (scaleProgress >= 1)
                    {
                        transform.localScale = initialScale;
                        readyToScale = false;
                    }
                    break;
                case ScaleState.Out:
                    transform.localScale = Vector3.Lerp(initialScale * squeezeAmount, initialScale, Console.Ease(scaleProgress, Console.EaseMode.EaseOut));
                    scaleProgress -= Time.deltaTime / scaleTime;
                    if (scaleProgress <= 0)
                    {
                        transform.localScale = initialScale * squeezeAmount;
                        readyToScale = false;
                    }
                    break;
            }
        }
    }

    public void MouseWheelCancel()
    {
        if (Input.GetMouseButton(0) && Input.mouseScrollDelta == Vector2.zero)
        {
            sRect.scrollSensitivity = originalSensitivity;
        }
        else
        {
            sRect.scrollSensitivity = 0;
        }
    }

    void OffscreenCheck()
    {
        Vector3 localScale = actualWindow.transform.localScale;

        float offsetY = actualWindow.localPosition.y + (actualWindow.rect.height* localScale.y / 2) - 10;

        RectTransform screenTransform = sRect.viewport;

        float horizontalScreenBorder = screenTransform.rect.width / 2 + (actualWindow.rect.width * localScale.x / 2);

        float verticalScreenBorder = screenTransform.rect.height / 2;

        if (Mathf.Abs(actualWindow.localPosition.x) > horizontalScreenBorder - 5)
        {
            horizontallyOffscreen = true;
        }
        if (Mathf.Abs(offsetY) > verticalScreenBorder)
        {
            verticallyOffscreen = true;
        }
        if (Mathf.Abs(actualWindow.localPosition.x) < horizontalScreenBorder - 15)
        {
            horizontallyOffscreen = false;
        }
        if (Mathf.Abs(offsetY) < verticalScreenBorder - 10)
        {
            verticallyOffscreen = false;
        }
    }

    bool isObscured()
    {
        for (int i = transform.GetSiblingIndex()+1; i < transform.parent.childCount; i++)//for each game object in front of me
        {
            if(transform.parent.GetChild(i).TryGetComponent(out DraggableWindow d))//if theres a draggable window in front of me
            {
                if (d.obscurer)//if this draggable window is marked as an obscurer
                {
                    return true;//i am being obscured
                }
            }
        }
        return false;//there was not an obscurer in my way
    }

    void SetAlpha()
    {
        CanvasGroup myCanvasGroup = GetComponent<CanvasGroup>();

        //Debug.Log(gameObject.name +" : "+ transform.GetSiblingIndex());

        if (isObscured())
        {
            myCanvasGroup.alpha = 0.8f;
        }
        else
        {
            myCanvasGroup.alpha = 1;
        }
    }

    public void MoveToFront()
    {
        transform.SetAsLastSibling();
    }

    public void Flash(Color flashColor)
    {
        colourFlash f = flash.GetComponentInChildren<colourFlash>();

        f.flashColour = flashColor;
        f.flashCount = 0;
        f.Flash();     
    }

    public void Close()
    {
        ScaleOut();

        colourFade.FadeObject(gameObject);
        obscurer = false;
        if (FileUI.allOpenedWindows.Contains(gameObject))
        {
            FileUI.allOpenedWindows.Remove(gameObject);
        }
        OnClose.Invoke();
    }
     
    public void StartDrag()
    {
        beingDragged = true;
    }
    public void EndDrag()
    {
        beingDragged = false;
    }
}
