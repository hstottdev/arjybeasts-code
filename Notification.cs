using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Notification : MonoBehaviour
{
    public static Notification n;
    public GameObject messageObject;
    public GameObject closeButton;
    public TextMeshProUGUI messsage;
    public TextMeshProUGUI sender;
    Notification debugN;

    [HideInInspector]
    public UnityEvent OnNotificationClick;
    public UnityEvent OnClose;
    // Start is called before the first frame update
    void Awake()
    {
        n = this;
    }

    private void Start()
    {
        messageObject.SetActive(false);
    }

    public static void Send(string messageText, string senderName, Color notificationColor, UnityAction onClicked = null)
    {
        //Debug.Log("what");
        n.messageObject.SetActive(true);
        n.messageObject.GetComponent<Graphic>().color = notificationColor;
        n.messageObject.transform.SetAsLastSibling();
        n.messsage.text = messageText;
        n.sender.text = senderName;
        if (onClicked != null)
        {
            n.OnNotificationClick.RemoveAllListeners();
            n.OnNotificationClick.AddListener(onClicked);
        }
        ToggleCloseButton(true);

        AudioManager.PlaySound("SFX/Pause1",0.8f,1,0.05f);
    }

    private void Update()
    {
        n = this;
        debugN = n;
    }

    public static void ToggleCloseButton(bool toggle)
    {
        n.closeButton.SetActive(toggle);
    }

    public void UserClosed()
    {
        Close();
        OnClose.Invoke();
    }

    public void Close()
    {
        if(messageObject != null)
        {
            if (messageObject.activeInHierarchy)
            {
                colourFade.FadeObject(Instantiate(messageObject, messageObject.transform.parent), true);//make a copy that fades and destroys
                messageObject.SetActive(false);//disable the original
            }
        }
    }

    public void Clicked()
    {
        OnNotificationClick.Invoke();
        OnNotificationClick.RemoveAllListeners();       
        Close();
    }
}
