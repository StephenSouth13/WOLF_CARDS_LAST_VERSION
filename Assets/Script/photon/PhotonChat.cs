using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class PhotonChat : MonoBehaviourPunCallbacks, IOnEventCallback
{
    
    public TMP_InputField inputField;
    public Button send;
    public Transform ChatDisplay;
    public Transform ChatWolfDisplay;
    public GameObject MessagePrefab;
    public GameObject normalObject;
    public GameObject wolfObject;
    private const byte ChatEventCode = 1;
    private const byte ChatWolfEventCode = 2;

    public Button normalChat;
    public Button WolfChat;

    private List<string> chatHistory = new List<string>();
    private List<string> chatWolfHistory = new List<string>();
    public bool DisplayWolfChat = false;
    private GamePlayManager editGame;
    private FirebaseLogin fblogin;
    public Action SystemMessage;

    private string NameUser;
    // normal chat
    public void HandleSystemMessageAll(string message)
    {
        string fullms = $"<color=red>[<b>Quản trò</b>] : </color>{message}";
        PhotonNetwork.RaiseEvent(ChatEventCode, fullms, new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        }, SendOptions.SendReliable);
    }
    public void HandleSystemMessageTarget(string message, int TargetActorNumber)
    {
        string fullms = $"<color=red>[<b>Quản trò</b>] : </color>{message}";
        PhotonNetwork.RaiseEvent(ChatEventCode, fullms, new RaiseEventOptions
        {
            TargetActors = new int[] { TargetActorNumber }
        }, SendOptions.SendReliable);
    }
    // wolf chat
    public void HandleSystemMessageAllWolf(string message)
    {
        string fullms = $"<color=red>[<b>Quản trò</b>] : </color>{message}";
        PhotonNetwork.RaiseEvent(ChatWolfEventCode, fullms, new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        }, SendOptions.SendReliable);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        editGame = FindFirstObjectByType<GamePlayManager>();

        NameUser = PlayerPrefs.GetString("NameUser", "Unknown");

        inputField.onEndEdit.AddListener(HandleSubmit);
        send.onClick.AddListener(() => SendOnClick());
        PhotonNetwork.AddCallbackTarget(this);
    }
    public void SendOnClick()
    {
        if (!DisplayWolfChat)
        {
            SendMessage();
        }
        else
        {
            SendWolfMessage();
        }
    }
    public void OnChatWolf() // gán vào button đêm
    {
        DisplayWolfChat = true;
        wolfObject.SetActive(true);
        normalObject.SetActive(false);
    }
    public void OnNormalChat() // gán vào button ngày
    {
        DisplayWolfChat = false;
        wolfObject.SetActive(false);
        normalObject.SetActive(true);
    }
    public AvatarManager FindAvatarByActorNumber(int actorNumber) // tìm và trả về đúng đối tượng chứa script trong 1 scene
    {
        foreach (AvatarManager avm in FindObjectsByType<AvatarManager>(FindObjectsSortMode.None))
        {
            if (avm.photonView.Owner.ActorNumber == actorNumber)
                return avm;
        }
        return null;
    }
    public List<int> GetAllWolfActorNumbers() // lấy danh sách người chơi có roleType là sói
    {
        List<int> wolfActors = new List<int>();
        foreach (AvatarManager avm in FindObjectsByType<AvatarManager>(FindObjectsSortMode.None))
        {
            if (avm.role == roleType.Wolf)
            {
                wolfActors.Add(avm.photonView.Owner.ActorNumber);
            }
        }
        return wolfActors;
    }

    #region messageSend
    public void HandleSubmit(string text)
    {

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!DisplayWolfChat)
            {
                SendMessage();
            }
            else
            {
                SendWolfMessage();
            }
        }
    }
    [PunRPC]
    void ReceiveMessageFromClient(string message, int senderActorNumber)
    {
        if (string.IsNullOrEmpty(NameUser))
        {
            Debug.Log("Name User chưa được gán!!");
            return;
        }
        var OrderedPlayer = PhotonNetwork.PlayerList.OrderBy(Player => Player.ActorNumber).ToArray();

        int JoinOrder = editGame.GetPlayerIndexByActorNumber(senderActorNumber);
        string FullMessage = $"{NameUser}({JoinOrder}): {message}";

        chatHistory.Add(FullMessage);

        // Chỉ thêm vào chatHistory nếu người gửi không phải là chính MasterClient
        if (PhotonNetwork.LocalPlayer.ActorNumber != senderActorNumber)
        {
            if (!chatHistory.Contains(FullMessage))
            {
                chatHistory.Add(FullMessage);
            }
            if (chatHistory.Count > 20)
            {
                chatHistory.RemoveAt(0);
            }
        }


        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["ChatHistory"] = chatHistory.ToArray();
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log(FullMessage);
        // Chỉ MasterClient gửi lại tin nhắn tới tất cả người chơi

    }
    [PunRPC]
    void ReceiveWolfMessageFromClient(string message, int senderActorNumber)
    {
        if (string.IsNullOrEmpty(NameUser))
        {
            Debug.Log("Name User chưa được gán!!");
            return;
        }
        var OrderedPlayer = PhotonNetwork.PlayerList.OrderBy(Player => Player.ActorNumber).ToArray();
        int JoinOrder = editGame.GetPlayerIndexByActorNumber(senderActorNumber);
        string FullMessage = $"{NameUser}({JoinOrder}): {message}";

        chatWolfHistory.Add(FullMessage);

        // Chỉ thêm vào chatWolfHistory nếu người gửi không phải là chính MasterClient
        if (PhotonNetwork.LocalPlayer.ActorNumber != senderActorNumber)
        {
            if (!chatWolfHistory.Contains(FullMessage))
            {
                chatWolfHistory.Add(FullMessage);
            }
            if (chatWolfHistory.Count > 20)
            {
                chatWolfHistory.RemoveAt(0);
            }
        }


        ExitGames.Client.Photon.Hashtable props2 = new ExitGames.Client.Photon.Hashtable();
        props2["chatWolfHistory"] = chatWolfHistory.ToArray();
        PhotonNetwork.CurrentRoom.SetCustomProperties(props2);
        
        Debug.Log(FullMessage);
        // Chỉ MasterClient gửi lại tin nhắn tới tất cả người chơi

    }
    public void SendMessage() // local Client gửi tin nhắn cho tất cả người chơi trong phòng
    {
        AvatarManager controler = FindAvatarByActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        if (controler != null)
        {
            if (!controler.isAlive)
            {
                Debug.Log("Bạn đã bị cấm chat!!");
                inputField.text = "";
                return;
            }
            if (!controler.CanChat)
            {
                Debug.Log("Không thể chat vào ban đêm");
                inputField.text = "";
                return;
            }
            string message = inputField.text;
            if (!string.IsNullOrEmpty(message))
            {
                int SenderActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("ReceiveMessageFromClient", RpcTarget.All, message, SenderActorNumber);
                int IndexActor = editGame.GetPlayerIndexByActorNumber(SenderActorNumber);
                string fullMessage = $"{NameUser}({IndexActor}): {message}";
                PhotonNetwork.RaiseEvent(ChatEventCode, fullMessage, new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.All
                }, SendOptions.SendReliable);

                inputField.text = "";
            }
        }
        else
        {
            Debug.LogError("controler bị null");
        }
    }
    public void SendWolfMessage() // local Client gửi tin nhắn cho tất cả người chơi trong phòng
    {
        AvatarManager controler = FindAvatarByActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        if (controler != null && controler.role == roleType.Wolf)
        {
            if (!controler.isAlive)
            {
                Debug.Log("Bạn đã bị cấm chat!!");
                inputField.text = "";
                return;
            }
            string message = inputField.text;
            if (!string.IsNullOrEmpty(message))
            {
                List<int> wolfTarget = GetAllWolfActorNumbers();
                int SenderActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("ReceiveWolfMessageFromClient", RpcTarget.All, message, SenderActorNumber);
                int IndexActor = editGame.GetPlayerIndexByActorNumber(SenderActorNumber);
                string fullMessage = $"{NameUser}({IndexActor}): {message}";
                PhotonNetwork.RaiseEvent(ChatWolfEventCode, fullMessage, new RaiseEventOptions
                {
                    TargetActors = wolfTarget.ToArray()
                }, SendOptions.SendReliable);

                inputField.text = "";
            }
        }
        else
        {
            Debug.LogError("controler bị null");
        }
    }
    public override void OnMasterClientSwitched(Player newMasterPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if(PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ChatHistory", out object historyData))
            {
                string[] retored = (string[])historyData;
                chatHistory = new List<string>(retored);
                Debug.Log("Đã nhận lại lịch sử chat: " + chatHistory.Count + " dòng");
            }
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && chatHistory.Count > 0)
        {
            foreach (var msg in chatHistory)
            {
                PhotonNetwork.RaiseEvent(ChatEventCode, msg, new RaiseEventOptions { TargetActors = new int[] { newPlayer.ActorNumber } }, SendOptions.SendReliable);
            }
           
        }
    }
    private bool IsWolf()
    {
        AvatarManager avm = FindAvatarByActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        return avm != null && avm.role == roleType.Wolf;
    }

    public void OnEvent(EventData PhotonEvent)
    {
        if (PhotonEvent.CustomData == null)
        {
            Debug.Log("[Chat] CustomData null");
            return;
        }

        switch (PhotonEvent.Code)
        {
            case ChatEventCode:
                if (PhotonEvent.CustomData is string normalMessage)
                {
                    GameObject newMessage = Instantiate(MessagePrefab, ChatDisplay);
                    newMessage.GetComponentInChildren<TMP_Text>().text = normalMessage;
                }
                break;

            case ChatWolfEventCode:
                if (!IsWolf()) return; // ❌ Không phải sói thì bỏ qua
                if (PhotonEvent.CustomData is string wolfMessage)
                {
                    GameObject WolfMessage = Instantiate(MessagePrefab, ChatWolfDisplay);
                    WolfMessage.GetComponentInChildren<TMP_Text>().text = wolfMessage;
                }
                break;

            default:
                Debug.Log("Không hỗ trợ PhotonEventCode: " + PhotonEvent.Code);
                break;
        }
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    private void ClearChatDisplay()
    {
        foreach(Transform child in ChatDisplay)
        {
            Destroy(child.gameObject);
        }
    }
    public override void OnLeftRoom()
    {
        ClearChatDisplay();        // Xóa clone trong ChatDisplay
        chatHistory.Clear();       // Nếu là master thì xóa luôn bộ nhớ
        chatWolfHistory.Clear();
    }
    #endregion
    #region GameMessage
    public void StartCountdown() // nút button startgame gọi đầu tiên
    {
        StartCoroutine(CountdownCoroutine());
    }
    IEnumerator CountdownCoroutine()
    {
        if (PhotonNetwork.IsMasterClient)
        {

            PhotonNetwork.CurrentRoom.IsOpen = false; // Khóa phòng, không cho người chơi mới tham gia
            PhotonNetwork.CurrentRoom.IsVisible = false; // Ẩn phòng khỏi danh sách phòng
            string[] countdownMessages =
            {
            "<color=blue><b>Trò chơi bắt đầu sau: </b></color>",
            "<color=red>5</color>",
            "<color=red>4</color>",
            "<color=red>3</color>",
            "<color=red>2</color>",
            "<color=red>1</color>",
            "<color=green><b>Bắt đầu!</b></color>"
            };

            foreach (var msg in countdownMessages)
            {
                PhotonNetwork.RaiseEvent(ChatEventCode, msg, new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.All
                }, SendOptions.SendReliable);
                yield return new WaitForSeconds(1f);
            }
            editGame.OnStartGame();
            
            editGame.MasterRpcRoleToALL();
        }
    }
    
    #endregion
}
