using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

using System.Security.Cryptography.X509Certificates;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Hashtable = ExitGames.Client.Photon.Hashtable;



public class GamePlayManager : MonoBehaviourPunCallbacks
{

    private AvatarManager avtManager; // lấy các trường dữ liệu của script
    private PhotonChat ptChat; // lấy các trường dữ liệu của script
    private OpenCard opCard;

    public Dictionary<int, roleType> PlayerRoles = new Dictionary<int, roleType>(); // cần reset      -------------------------
    public Dictionary<int, string> DicCardName = new Dictionary<int, string>();
    public TextMeshProUGUI statusCountText;
    public string saveTatusCount; // master sẽ lưu trước rồi phát lại tất cả 
    #region SetUp Characters

    private Dictionary<Sprite, string> spriteCharacter = new Dictionary<Sprite, string>();
    private Dictionary<string, Action> actionCharacter = new Dictionary<string, Action>();
    private Dictionary<string, roleType> rolesByName = new Dictionary<string, roleType>();
    private Dictionary<int, int> PlayerIndex = new Dictionary<int, int>();
    private List<string> normarlName = new List<string> { "Dân Làng"};

    private List<string> nameAllVilagers = new List<string>
    {
        "Phù Thủy", "Trưởng Làng", "Cupid", "Thợ Săn", "Đạo Tặc","Kẻ Thế Thân", "Người Bệnh","Tiên Tri", "Bảo Vệ","kẻ Phản Bội(Dân)"
    };
    private List<string> nameWolf = new List<string>
    {
        "Sói Săn"
    };
    private List<string> nameAllWolf = new List<string>
    {
        "Sói Đầu Đàn", "Sói Bệnh", "Sói con"
    };
    private List<string> nameAllSolo = new List<string>
    {
        "Người Thổi Sáo", "Kẻ Chán Đời", "Sói Xám", "Gương Nhân Bản"
    };
    void SetUpNameToRole(List<string> name, roleType role)
    {
        foreach (string names in name)
        {
            rolesByName[names] = role;
        }
    }

    void SetUpNameToSprite(Sprite[] sprite, List<string> name)
    {
        
        for(int i = 0; i < sprite.Length; i++)
        {
            spriteCharacter[sprite[i]] = name[i];
        }
    }
    void SetUpAllActionToAllName()
    {
        actionCharacter["Dân Làng"] = villagerFT;
        actionCharacter["Tiên Tri"] = ProphetFT;
        actionCharacter["Bảo Vệ"] = GuardianFT;
        actionCharacter["Sói Săn"] = WolfFT;
    }
    private void Awake()
    {

        avtManager = FindFirstObjectByType<AvatarManager>();
        ptChat = FindFirstObjectByType<PhotonChat>();
        opCard = FindFirstObjectByType<OpenCard>();
        SetUpAllActionToAllName();

        SetUpNameToSprite(NormalVilager, normarlName);
        SetUpNameToSprite(Mode3Villagers, nameAllVilagers);
        SetUpNameToSprite(Wolf, nameWolf);
        SetUpNameToSprite(Mode2Wolf, nameAllWolf);
        SetUpNameToSprite(Mode2Solo, nameAllSolo);

        SetUpNameToRole(normarlName, roleType.Villager);
        SetUpNameToRole(nameAllVilagers, roleType.Villager);
        SetUpNameToRole(nameWolf, roleType.Wolf);
        SetUpNameToRole(nameAllWolf, roleType.Wolf);
        SetUpNameToRole(nameAllSolo, roleType.Solo);
    }
    public void ExecuteAction(string CharacterName)
    {
        if (actionCharacter.TryGetValue(CharacterName, out Action action))
        {
            action.Invoke();
        }
        else
        {
            Debug.Log($"Không tìm thấy hành động cho nhân vật : {CharacterName}");
        }
    }
    public string GetName(Sprite sprite)
    {
        return spriteCharacter.TryGetValue(sprite, out string characterName) ? characterName : "Không xác định";
    }
    public roleType getRole(string name)
    {
        if(rolesByName.TryGetValue(name, out roleType role))
        {
            return role;
        }
        Debug.LogWarning($"Ko tìm thấy role cho nhân vật {name}");
        return roleType.UnKnown;
    }
    #endregion

    bool isFullyInitilized = false;

    public Button StartGame;
    public Button Card;

    // list ban đầu cho 5 người chơi
    public Sprite[] NormalVilager; // chỉ 1 dân thường (tính chất lặp lại)        --- (gán tên)
    public Sprite[] Villagers; // 1 bảo vệ, 1 tiên tri 
    public Sprite[] Mode2Villagers; // (mode 8 người trở lên)full dân trừ kẻ phản bội và dân thường (tính chất độc nhất)
                                    
    public Sprite[] Mode3Villagers; // Thêm kẻ phản bội => full dân trừ dân thường--- (gán tên)
                                    // (gán sprite thứ tự theo tên của list) 

    public Sprite[] Wolf; // 1 sói săn (tính chất lặp lại)                        --- (gán tên)
    public Sprite[] Mode2Wolf; // (mode 8 người trở lên) add full sói trừ sói săn --- (gán tên)
                               // (gán sprite thứ tự theo tên của list) 

    // list khi 6 người chơi: 0 add;
    // list khi 7 người chơi: 
    // list Khi 8 người chơi: Add full sói dân trừ kẻ phản bội(dân) và gương(solo)
    public Sprite[] Solo; // 1 sói xám, 1 Kẻ chán đời, 1 Người thổi sáo 
    public Sprite[] Mode2Solo; // thêm gương => full solo (sau 10 người chơi)     --- (gán tên)
                               // (gán sprite thứ tự theo tên của list) 

    private List<Action> gameModes = new List<Action>();
    // Sau 10 người Add kẻ phản bội(dân), gương

    // CHẾ ĐỘ: 1: nhiều dân, nhiều solo
    // dân 90(4), 70(5,5,5,6,7), 60(7,8,9,8,9)
    // sói 10(1), 20(1,2,2,2,2), 30(3,3,3,4,4)
    // solo       10(0,0,1,1,1), 10(1,1,1,2,2)

    // CHẾ ĐỘ 2: Nhiều sói, ít solo;
    // dân 90(4), 70(5,5,6,7,7), 60(8,8,9,9,10)
    // sói 10(1), 20(1,2,2,2,3), 30(3,4,4,4,4)
    // solo        0(0,0,0,0,0), 10(0,0,0,1,1)

    // CHẾ ĐỘ 3: sói nhiều,tăng nhẹ solo, dân nhiều vai mạnh giảm dân thường(hoặc bằng hết)
    // dân 90(4), 70(4,4,5,6,6), 60(7,7,7,8,9)
    // sói 10(1), 20(2,2,2,2,3), 30(3,4,4,4,4)
    // solo        0(0,1,1,1,1), 10(1,1,2,2,2)

    /* NOTE****
        - cần khóa phòng khi bắt đầu -- (ok xong)
        - CountDown trước khi khóa (tránh việc vừa khóa lại có người vào trước 1s) -- (ok xong)
        - tính lặp lại của dân thường và sói thường , tính độc nhất các thẻ đặt biệt; -- (OK xong)
        - tạo class Nhân vật lưu (tên , sprite() , Action()) (không cần thiết) (cần khi có firebase)
        - Cần countDown để chuyển bối cảnh chơi (tối , sáng , vote) -- (Ok xong)
        - cần chế độ vote trên Prefab (?) -- (Ok xong)
        - khi nhấn button từ avatarManager.cs thì sẽ kiểm tra có phải chính mình hay không và  gọi các acTion bên GamePlayManager(tất cả) -- (Ok xong)
            + hiện tại action đang dc gọi ngay khi gán Sprite, Giờ cần set điều kiện để thực hiện trong thời gian quy định -- (Ok xong)
        - sẽ dùng case when để thêm điều kiện để kiểm soát thời gian thực hiện action -- (Ok xong)
        - bên trong CountDown sẽ thiết lập 3 trường dữ liệu và isPlaying làm kiểm soát vòng lặp While(true) -- (ok xong)

        - test lại onclick bằng lệnh đơn giản với avatar -- (ok xong đã thay đổi 1 chút là để object thêm component buton còn button thì xóa component thành markImage)
        - thực hiện vote người chơi khi có Phase "vote" sẽ đổi numberOfAction = 0; -- (ok xong)
        - numberOfActioc sẽ được gán vào khi bắt đầu chia bài  -- (ok xong)
            được để bên trong code Action() của từng thẻ bài khi gọi invoke sẽ tự dộng gán-- (ok xong)
        - cần có bộ tính = list để đếm xem có bao nhiêu sói bao nhiêu dân và bao nhiêu solo để tiến hành kết thúc ván game -- (Ok xong)
        - Wolf cần có chức năng cắn vào phase "nighht" và 2 khung chat ngày là đêm riêng biệt
            + cần có thông báo ai bị giết vào "Day" tiếp theo;
            + cơ chế xử lý chặn người chơi == isAlive(); 
            + bên PhotonChat sẽ có bool Avatarmanager.isAlive(true) để kiểm soát việc chat nếu người 
                chơi bị loại điều này sẽ thực hiện trong action của GamePlayManager == false -- (Ok xong)
        - Button của avatar người chơi sẽ thực hiện các chức năng : 
            + chức năng nhận diện người chơi onClick trên avatar -- (ok xong)
            + xem thông tin người chơi khi chưa bắt đầu game <để xem xét có thời gian thì thêm vào>
            + vote người chơi -- (ok xong)
            + chọn người giết cứu và bảo vệ các chức năng nhờ listener(ActionOnclickAvatar);
    
        - các giá trị cần reset khi game kết thúc :
            + MasterVoteCount, votecount , BackupNumberAction, OutPlayer , ifAlive

        - Làm 1 hàm reset các giá trị trên
            + gọi nó trong RPC stopGameForAll
                +Khi xét được điều kiện win trong checkWinCondition sẽ gọi stopGameForAll để dừng vòng lặp kèm reset giá trị;
                    + 1 hàm phân biệt ai win ai thua để hiện fanel 
                    + hiển thị panel WIN / Lose, mở lại visible của phòng;
                        + Nút tiếp tục, nút out phòng ;
        - Cần Xác định người chơi đã bị loại khi vote hoặc cắn thì sẽ 
           
            +tạm thời tắt chức năng button của người đó(ở vị trí đó) như chat và vote ( ok xong)
            +Chỉ còn chức năng xem  thông tin người chơi () (Tạm thời chưa cần)
            +Các người chơi khác sẽ thấy avatar của người bị loại mờ đi và không thực hiện Action lên người đó được. (ok xong)


        - Lượt vote của sói sẽ phải thông qua người chơi đó có đang được bảo vệ hay không, và có là Solo ko(sói ko thể cắn solo) (ok xong)
            + nếu có thì chặn lượt vote(cắn) và thông báo lên khung chat của sói sau khi hết Phase "Night"; 
            + nếu không thì loại người chơi đó và kích hoạt chức năng riêng của người loại nếu có

   
        
        -làm panel Sói,Dân,Solo win kèm 2 button chơi tiếp, thoát phòng; (ok xong ) 
        -tìm cách tách hộp thoại ngày đêm (chức năng sói) vote riêng của sói; (OK xong)
            + sẽ có 2 phần content;
            + dùng buttton để chuyển đổi content
            + khi người chơi gửi msg thì kiểm tra đang ở content nào 
                + content ngày là mặc định 
        - Chỉnh lại voteMap: 
            + hiện tại người chơi client khi vote sẽ bị lưu lại và ở lượt Phase vote kế tiếp nếu vote cùng 1 người thì sẽ bị chặn do currentVote == victimActor (Ok xong)
   
   
        - cần Phải xác định lại số thứ tự người chơi trong phòng vì Khi clone PhotonView sẽ vướt quá số người trong phòng nếu ra vào nhiều lần (Ok xong)

        - Thông báo lên khung chat người chơi bị loại vì lí do nào  (Ok xong)
            + cần phải có 
        - hiện tại sói vote sói cũng bị loại (tạm cho là xong. chỉnh ở WolfVote)
     */
    /* NOTE cái cần thêm hôm sau:
        - chuyển đồi hết từ json sang firebase
        - kết nnooisbutton card và đúng ảnh của nó
            + kèm thêm các đoạn text thông báo sói, dân, solo còn bao nhiêu
        - Ghép nhạc nền và âm thanh hành động như click, sói cắn , bảo vệ , tiên tri , chuyển cảnh panel, chuyển đổi ngày đêm
            + làm thêm điều chỉnh âm thanh 
    */
    #region CountDownTime
    public Image DayImage;
    public Image NightImage;
    public TextMeshProUGUI CountDownTimer;
    public Button StopGame; // Default trường hợp vòng lặp vô tận
    private int CountPlayers;

    private bool isPlaying = true;


    private List<string> villagerCount = new List<string>();
    private List<string> wolfCount = new List<string>();
    private List<string> soloCount = new List<string>();

    private string gamePhase = "Night";

    public Image day;
    public Image vote;
    public Image night;
    public float dayTime = 30;
    public float voteTime = 15;
    public float nightTime = 20;
    private string timeRemaining = "";
    private Coroutine phaseCoroutine;
    private bool ControlSkill = false;
    public IEnumerator PhaseLoop()
    {
        while (isPlaying)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("MasterSetAllCanChatFalse", RpcTarget.All);
                photonView.RPC("RPC_SetPhase", RpcTarget.All, "Night", nightTime, Color.blue.r, Color.blue.g, Color.blue.b, Color.blue.a);
                Debug.Log($"NumberOfAction Đã đổi sang : {NumberOfAction}");
                yield return StartCoroutine(Countdown(nightTime));
                if (!isPlaying) yield break;
                EndVotingWolf();
                PointedOff();
                photonView.RPC("MasterSetAllCanChatTrue", RpcTarget.All);
                ResetSkillByOnNight();
                photonView.RPC("RPC_SetPhase", RpcTarget.All, "Day", dayTime, Color.green.r, Color.green.g, Color.green.b, Color.green.a);
                Debug.Log($"NumberOfAction Đã đổi sang : {NumberOfAction}");
                yield return StartCoroutine(Countdown(dayTime));
                if (!isPlaying) yield break;

                photonView.RPC("RPC_SetPhase", RpcTarget.All, "Vote", voteTime, Color.red.r, Color.red.g, Color.red.b, Color.red.a);
                Debug.Log($"NumberOfAction Đã đổi sang : {NumberOfAction}");
                yield return StartCoroutine(Countdown(voteTime));

                EndVoting();
                PointedOff();
                
            }
            
        }
    }
    [PunRPC]
    void MasterSetAllCanChatTrue()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)

        {
            int actorNumber = player.ActorNumber;
            AvatarManager avm = FindAvatarByActorNumber(actorNumber);
            if (avm != null)
            {
                avm.CanChat = true;
            }
        }
    }
    [PunRPC]
    void MasterSetAllCanChatFalse()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)

        {
            int actorNumber = player.ActorNumber;
            AvatarManager avm = FindAvatarByActorNumber(actorNumber);
            if (avm != null)
            {
                avm.CanChat = false;
            }
        }
    }
    [PunRPC]
    public void RPC_SetPhase(string phase, float duration, float r,float g,float b, float a)
    {
        Color receiveColor = new Color(r, g, b, a);
        setPhase(phase, receiveColor);
        StartCoroutine(Countdown(duration));
    }
    [PunRPC]
    public void RPC_SetDefaultPhase(string phase, float r, float g, float b, float a)
    {
        Color receiveColor = new Color(r, g, b, a);
        setPhase(phase, receiveColor);
        
    }
    private IEnumerator Countdown(float duration)
    {
        CountDownTimer.gameObject.SetActive(true);
        double start = PhotonNetwork.Time;
        while (PhotonNetwork.Time - start < duration)
        {
            float timeLeft = (float)(duration - (PhotonNetwork.Time - start));
            UpdateTimerUI(timeLeft);
            yield return null;
        }
    }
    void UpdateTimerUI(float timeLeft)
    {
        CountDownTimer.text =  Mathf.CeilToInt(timeLeft).ToString();
    }
    private void setPhase(string phase,Color color)
    {
        gamePhase = phase;
        
        CountDownTimer.color = color;
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        AvatarManager avm = FindAvatarByActorNumber(localActor);
        switch (gamePhase)
        {
            case "Day":
                DayImage.gameObject.SetActive(true);
                NightImage.gameObject.SetActive(false);
                day.gameObject.SetActive(true);
                night.gameObject.SetActive(false);
                vote.gameObject.SetActive(false);
                break;
            case "Night":

                if (avm.isAlive)
                {
                    ControlSkill = true;
                    if (NumberOfAction != 0)
                    {
                        BackupNumberAction = NumberOfAction;
                    }
                }
                DayImage.gameObject.SetActive(false);
                NightImage.gameObject.SetActive(true);
                if (avm.isAlive && BackupNumberAction != 0 && NumberOfAction == 0)
                {
                    NumberOfAction = BackupNumberAction;
                    
                }
                
                day.gameObject.SetActive(false);
                night.gameObject.SetActive(true);
                vote.gameObject.SetActive(false);
                break;
            case "Vote":
                if (avm.isAlive)
                {
                    
                    NumberOfAction = 0;
                }
                day.gameObject.SetActive(false);
                night.gameObject.SetActive(false);
                vote.gameObject.SetActive(true);
                break;
            default:
                day.gameObject.SetActive(false);
                night.gameObject.SetActive(false);
                vote.gameObject.SetActive(false);
                CountDownTimer.gameObject.SetActive(false );
                break;
        }
        Debug.Log($"Bắt đầu pha: {phase}");
    }
    public void stopGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Đã dừng vòng lặp cho tất cả người chơi ");

            photonView.RPC("StopGameForAll", RpcTarget.All);

        }
    }
    [PunRPC]
    private void StartCountDownForAll()
    {
        isPlaying = true;
        Debug.Log("Game bắt đầu đếm ngược");
        if (PhotonNetwork.IsMasterClient)
        {
            phaseCoroutine = StartCoroutine(PhaseLoop());
        }
    }
    [PunRPC]
    private void StopGameForAll()
    {
        
        isPlaying = false;
        CountDownTimer.text = timeRemaining;
        photonView.RPC("RPC_SetDefaultPhase",RpcTarget.All, "default", Color.black.r, Color.black.g, Color.black.b, Color.black.a);
        if (phaseCoroutine != null) // Kiểm tra nếu Coroutine tồn tại
        {
            StopCoroutine(phaseCoroutine);
            Debug.Log("Đã dừng vòng lặp PhaseLoop!");
            phaseCoroutine = null; // Reset giá trị
        }



    }
    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CountPlayers = PhotonNetwork.PlayerList.Length;
        
        OnOffButtonStart();
    }
    private int GetNextAvailbleIndex()
    {
        int index = 1;
        HashSet<int> UserIndices = new HashSet<int>(PlayerIndex.Values);

        while (UserIndices.Contains(index))
        {
            index++;
        }
        return index;
    }
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int index = 1; // hoặc GetNextAvailableIndex()

            // 1. Gán custom property
            Hashtable props = new Hashtable { { "PlayerIndex", index } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            // 2. Gán luôn vào Dictionary nếu có
            if (PlayerIndex != null)
            {
                PlayerIndex[PhotonNetwork.LocalPlayer.ActorNumber] = index;
            }

            Debug.Log("Chủ phòng đã tự gán PlayerIndex và thêm vào Dictionary.");
        }
    }
    public int GetPlayerIndexByActorNumber(int actorNumber) // trả về value dự vào key là actorNumber
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
            {
                if (p.CustomProperties.TryGetValue("PlayerIndex", out object indexObj))
                {
                    return (int)indexObj;
                }
            }
        }
        return -1; // Không tìm thấy hoặc chưa được gán
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int nextIndex = GetNextAvailbleIndex();
            PlayerIndex[newPlayer.ActorNumber] = nextIndex;

            Hashtable props = new Hashtable { { "PlayerIndex", nextIndex } };
            newPlayer.SetCustomProperties(props);

            Debug.Log($"[Master] Gán PlayerIndex = {nextIndex} cho {newPlayer.NickName}");

            
            
        }
        CountPlayers = PhotonNetwork.PlayerList.Length;
        Debug.Log($"Người chơi :{newPlayer.NickName} đã vào phòng.");
        OnOffButtonStart();
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        
        if (PhotonNetwork.IsMasterClient)
        {
            PlayerIndex.Remove(otherPlayer.ActorNumber);
            
            
        }
        CountPlayers = PhotonNetwork.PlayerList.Length;
        Debug.Log($"Người chơi :{otherPlayer.NickName} đã rời phòng, số người hiện tại : {CountPlayers}.");
        OnOffButtonStart();
    }
    private void OnOffButtonStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartGame.gameObject.SetActive(CountPlayers >= 5);
        }
    }
    //public void CheckPlayersCount(int CPlayers)
    //{
    //    if (CPlayers == 5)
    //    {

    //    }
    //}
    public void OnStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int randomMode = UnityEngine.Random.Range(0, 2);
            switch(randomMode)
            {
                case 0:
                    Mode1();
                    break;
                case 1:
                    Mode2();
                    break;
                case 2:
                    Mode3();
                    break;
                default:
                    Debug.Log("Lỗi mode chơi");
                    break;
            }
            StartGame.gameObject.SetActive(false);
            photonView.RPC("StartCountDownForAll", RpcTarget.All);
            photonView.RPC("SetActiveSkill", RpcTarget.All);
        }
    }
    public void Mode1()
    {
        List<Sprite> roles = new List<Sprite>();
        HashSet<Sprite> SelectedCharacter = new HashSet<Sprite>();
        if (CountPlayers < 5)
        {
            Debug.Log("[OnStartGame] Lỗi thiếu người chơi !!! Lỗi hiển thị nút Start");
            return;
        }
        else
        {
            switch (CountPlayers)
            {
                case 5:
                    for (int i = CountPlayers; i > CountPlayers - 2; i--)
                    {
                        roles.Add(GetWeightedRandomSprite(NormalVilager, new int[] { 1 }));
                        
                    }
                    for (int i = CountPlayers; i > CountPlayers - 2; i--)
                    {
                        Sprite chosenSprite;
                        do
                        {
                            chosenSprite = GetWeightedRandomSprite(Villagers, new int[] { 1, 1 });
                        }while( SelectedCharacter.Contains(chosenSprite) );
                        SelectedCharacter.Add(chosenSprite);
                        roles.Add(chosenSprite);
                        
                    }
                    roles.Add(GetWeightedRandomSprite(Wolf, new int[] { 1 }));
                    

                    Shuffle(roles);
                    photonView.RPC("AssignRoles", RpcTarget.All, roles.ConvertAll(sprite => sprite.name).ToArray());

                    break;
                default:
                    if (CountPlayers > 15)
                    {
                        Debug.Log("[OnStartGame] Lỗi số đém người chơi !!! ");
                    }
                    break;
            }
        }
    }
    public void Mode2()
    {
        List<Sprite> roles = new List<Sprite>();
        HashSet<Sprite> SelectedCharacter = new HashSet<Sprite>();
        if (CountPlayers < 5)
        {
            Debug.Log("[OnStartGame] Lỗi thiếu người chơi !!! Lỗi hiển thị nút Start");
            return;
        }
        else
        {
            switch (CountPlayers)
            {
                case 5:
                    for (int i = CountPlayers; i > CountPlayers - 2; i--)
                    {
                        roles.Add(GetWeightedRandomSprite(NormalVilager, new int[] { 1 }));
                    }
                    for (int i = CountPlayers; i > CountPlayers - 2; i--)
                    {
                        Sprite chosenSprite;
                        do
                        {
                            chosenSprite = GetWeightedRandomSprite(Villagers, new int[] { 1, 1 });
                        } while (SelectedCharacter.Contains(chosenSprite));
                        SelectedCharacter.Add(chosenSprite);
                        roles.Add(chosenSprite);
                    }
                    roles.Add(GetWeightedRandomSprite(Wolf, new int[] { 1 }));

                    Shuffle(roles);
                    photonView.RPC("AssignRoles", RpcTarget.All, roles.ConvertAll(sprite => sprite.name).ToArray());

                    break;
                default:
                    if (CountPlayers > 15)
                    {
                        Debug.Log("[OnStartGame] Lỗi số đém người chơi !!! ");
                    }
                    break;
            }
        }
    }
    public void Mode3()
    {
        List<Sprite> roles = new List<Sprite>();
        HashSet<Sprite> SelectedCharacter = new HashSet<Sprite>();
        if (CountPlayers < 5)
        {
            Debug.Log("[OnStartGame] Lỗi thiếu người chơi !!! Lỗi hiển thị nút Start");
            return;
        }
        else
        {
            switch (CountPlayers)
            {
                case 5:
                    for (int i = CountPlayers; i > CountPlayers - 2; i--)
                    {
                        roles.Add(GetWeightedRandomSprite(NormalVilager, new int[] { 1 }));
                    }
                    for (int i = CountPlayers; i > CountPlayers - 2; i--)
                    {
                        Sprite chosenSprite;
                        do
                        {
                            chosenSprite = GetWeightedRandomSprite(Villagers, new int[] { 1, 1 });
                        } while (SelectedCharacter.Contains(chosenSprite));
                        SelectedCharacter.Add(chosenSprite);
                        roles.Add(chosenSprite);
                    }
                    roles.Add(GetWeightedRandomSprite(Wolf, new int[] { 1 }));

                    Shuffle(roles);
                    photonView.RPC("AssignRoles", RpcTarget.All, roles.ConvertAll(sprite => sprite.name).ToArray());

                    break;
                default:
                    if (CountPlayers > 15)
                    {
                        Debug.Log("[OnStartGame] Lỗi số đém người chơi !!! ");
                    }
                    break;
            }
        }
    }
    #region Actions
    private List<int> VoteCount = new List<int>();
    private Dictionary<int, int> MasterVoteCount = new Dictionary<int, int>();

    int currentvote = -1;
    private int NumberOfAction;
    private int BackupNumberAction; // lưu tạm giá trị NumberOfAction khi hết time thì gán lại cho numberOfAction 
                                    // set các logic hành động cho Button avatar
    private Dictionary<int, int> lastPointedAvt = new Dictionary<int, int>();
    private Dictionary<int, int> voteMap = new Dictionary<int, int>(); // dùng để lưu nhưng gì ShowBoardTextVote làm, dùng lại để xóa ở các client khác
    private int OutPlayer = 0;
    private bool haschossenTarget = false;

    public void OnActionButtonClicked(int victimActorNumber) // avtar sẽ gọi hàm này đầu tiên khi nhấn nút
    {
        int attacker = PhotonNetwork.LocalPlayer.ActorNumber;

        // Gửi RPC tới avatar bị nhấn, cần tìm avatar tương ứng
        AvatarManager victim = FindAvatarByActorNumber(victimActorNumber);
        if (victim != null && victim.isAlive)
        {
            if (NumberOfAction == -1)
            {
                Debug.Log($"[OnActionButtonClicked] Bạn đã bị chặn Action!!");
            }
            else
            {
                victim.photonView.RPC("OnAvatarClicked", RpcTarget.All, attacker, victimActorNumber); // chỉ là 1 câu thông báo bên script AvatarManager
                ActionOnclickAvatar(victimActorNumber , victim);
            }
        }
        else
        {
            Debug.Log($"[Client] Người chơi này đã bị loại isAlive = {victim.isAlive}");
        }
    }
    public AvatarManager FindAvatarByActorNumber(int actorNumber)
    {
        foreach (AvatarManager avm in FindObjectsByType<AvatarManager>(FindObjectsSortMode.None))
        {
            if (avm.photonView.Owner.ActorNumber == actorNumber)
                return avm;
        }
        return null;
    }
    #region text & board & point VOTE
    public void PointedPublic(int actorNumber)                           // thêm 1 cái off ở đây
    {
        photonView.RPC("ShowVotePointed", RpcTarget.All,  actorNumber);
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        int actorPlayer = GetPlayerIndexByActorNumber(actorNumber);
        photonView.RPC("ShowBoardTextVote", RpcTarget.All, localActor, actorPlayer);
    }
    [PunRPC]
    void ShowVotePointed(int actorNumber)
    {
        int localPlayer = PhotonNetwork.LocalPlayer.ActorNumber;
        if (lastPointedAvt.ContainsKey(localPlayer))
        {
            int oldActor = lastPointedAvt[localPlayer];
            AvatarManager oldTarget = FindAvatarByActorNumber(oldActor);
            if (oldTarget != null)
            {
                oldTarget.pointed.gameObject.SetActive(false);
            }
        }

        AvatarManager newTarget = FindAvatarByActorNumber(actorNumber);
        if (newTarget != null)
        {
            newTarget.pointed.gameObject.SetActive(true);
            lastPointedAvt[localPlayer] = actorNumber;
        }
    }
    [PunRPC]
    void ShowBoardTextVote(int localActor, int actorNumber)
    {
        AvatarManager selfAvt = FindAvatarByActorNumber(localActor);
        if (selfAvt != null)
        {
            selfAvt.voteText.gameObject.SetActive(true);
            selfAvt.voteText.text = actorNumber.ToString(); // Ai mình vote
            selfAvt.board.gameObject.SetActive(true);
        }
        voteMap[localActor] = actorNumber;
    }
    public void PointedOnForWolf(int actorNumber)                           // thêm 1 cái off ở đây
    {
        photonView.RPC("ShowVoteWolfPointed", RpcTarget.All,  actorNumber);
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        int actorPlayer = GetPlayerIndexByActorNumber(actorNumber);
        photonView.RPC("ShowBoardTextWolfVote", RpcTarget.All, localActor, actorPlayer);
    }
    [PunRPC]
    void ShowVoteWolfPointed(int actorNumber)
    {
        int localPlayer = PhotonNetwork.LocalPlayer.ActorNumber;
        AvatarManager avm = FindAvatarByActorNumber(localPlayer);
        if (avm != null)
        {
            if (avm.role != roleType.Wolf) return;
            if (lastPointedAvt.ContainsKey(localPlayer))
            {
                int oldActor = lastPointedAvt[localPlayer];
                AvatarManager oldTarget = FindAvatarByActorNumber(oldActor);
                if (oldTarget != null)
                {
                    oldTarget.pointed.gameObject.SetActive(false);
                }
            }

            AvatarManager newTarget = FindAvatarByActorNumber(actorNumber);
            if (newTarget != null)
            {
                newTarget.pointed.gameObject.SetActive(true);
                lastPointedAvt[localPlayer] = actorNumber;
            }
        }
    }
    [PunRPC]
    void ShowBoardTextWolfVote(int localActor, int actorNumber)
    {
        
        AvatarManager selfAvt = FindAvatarByActorNumber(localActor);
        if (selfAvt != null)
        {

            if (selfAvt.role == roleType.Wolf && selfAvt.photonView.IsMine) // local 1 mình mình thấy
            {
                selfAvt.voteText.gameObject.SetActive(true);
                selfAvt.voteText.text = actorNumber.ToString(); 
                selfAvt.board.gameObject.SetActive(true);
            }
            
        }
        voteMap[localActor] = actorNumber;
        photonView.RPC("ShowBoardTextWolfVoteToAllWolf", RpcTarget.All, localActor, actorNumber);
    }
    int indexActorNumber; // trả về ActorNumber của người bị vote
    [PunRPC]
    void ShowBoardTextWolfVoteToAllWolf(int localActor, int actorNumber) // actorNumber là chỉ để hiển thị số thứ tự
    {
        
        int actorPlayer = PhotonNetwork.LocalPlayer.ActorNumber;
        // Nếu local không phải sói thì không quan tâm
        
        foreach(KeyValuePair<int, int > idx in PlayerIndex)
        {
            if(idx.Value == actorNumber)
            {
                indexActorNumber = idx.Key;
            }
        }
        AvatarManager selfAvt = FindAvatarByActorNumber(actorPlayer);
        if (selfAvt != null && selfAvt.role != roleType.Wolf) return;

        // Không hiển thị nếu chính mình là người đã vote
        if (localActor == indexActorNumber) return;

        // Hiển thị vote của đồng đội
        AvatarManager otherAvt = FindAvatarByActorNumber(localActor);
        if (otherAvt != null)
        {

            if (otherAvt.role == roleType.Wolf && !otherAvt.photonView.IsMine) 
            {
                otherAvt.voteText.gameObject.SetActive(true);
                otherAvt.voteText.text = actorNumber.ToString();
                otherAvt.board.gameObject.SetActive(true);

            }
            
        }

    }
    #region Clear 1 boardVote
    public void ClientSendMasterClearBoardTextVote(int localActor) // client gửi yêu cầu hủy bảng vote lên Master
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RequestMasterToClearVoteOnScene", RpcTarget.MasterClient, localActor);
        } else
        {
            photonView.RPC("Destroy1BoardTextVote", RpcTarget.All, localActor);
            Debug.Log($"[Master] Đã tự xóa voteBoard tại [{localActor}]");
        }
    }
    [PunRPC]
    void RequestMasterToClearVoteOnScene(int localActor) // Master nhận và thực hiện hủy bảng vote tại vị trí của client gửi
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log($"[Master] Đã nhận yêu cầu xóa voteBoard tại [{localActor}]");
        photonView.RPC("Destroy1BoardTextVote", RpcTarget.All, localActor);
    }
    [PunRPC]
    void Destroy1BoardTextVote(int localActor) // xóa 1 vị trí ở all client
    {
        AvatarManager selfAvt = FindAvatarByActorNumber(localActor);
        if (selfAvt != null)
        {
            selfAvt.voteText.gameObject.SetActive(false);
            selfAvt.voteText.text = "";
            selfAvt.board.gameObject.SetActive(false);
        }
        if (voteMap.Remove(localActor))
        {
            Debug.Log($"[Destroy1BoardTextVote] Đã remove voteMap[{localActor}]");
        }else
        {
            Debug.Log($"[Destroy1BoardTextVote] chưa remove voteMap[{localActor}]");
        }
        
    }
    #endregion 
    [PunRPC]
    void TurnOffBoardTextVote() // off all
    {
        foreach(var pair in voteMap)
        {
            int actor = pair.Key;
            AvatarManager selfAvt = FindAvatarByActorNumber(actor);
            if (selfAvt != null)
            {
                selfAvt.voteText.gameObject.SetActive(false);
                selfAvt.voteText.text = ""; // Ai mình vote
                selfAvt.board.gameObject.SetActive(false);
            }
        }
        
        voteMap.Clear();
    }
    public void PointedOff()
    {
        photonView.RPC("TurnOffPointed", RpcTarget.All);
        photonView.RPC("TurnOffBoardTextVote", RpcTarget.All);
    }
    [PunRPC]
    void TurnOffPointed() // all point
    {
        foreach(var pair in lastPointedAvt)
        {

            AvatarManager target = FindAvatarByActorNumber(pair.Value);
            if (target != null)
            {
                target.pointed.gameObject.SetActive(false);
            }
            AvatarManager selfAvt = FindAvatarByActorNumber(pair.Key);
            if (selfAvt != null)
            {
                selfAvt.voteText.gameObject.SetActive(false);
                selfAvt.voteText.text = "";
                selfAvt.board.gameObject.SetActive(false);
            }
        }
        lastPointedAvt.Clear();
    }
    #endregion
    public void ActionOnclickAvatar(int victimActor,AvatarManager avt)
    {
        Debug.Log($"[ActionOnclickAvatar] Đã được gọi, victimActor: {victimActor}");
        Debug.Log($"[ActionOnclickAvatar] Local: {PhotonNetwork.LocalPlayer.ActorNumber}, Owner: {photonView.Owner.ActorNumber}");

        // role
        switch (NumberOfAction)
        {
            case 0 when gamePhase == "Vote": // vote sẽ gán trực tiếp numberAction cho tất cả người chơi khi đến thời gian
                
                Vote(victimActor);
                
                Debug.Log("[ActionOnclickAvatar] Đã vào case 0");
                break;
            // các case khác sẽ là giá trị được gán ngay khi nhận sprite(thẻ bài)
            case 1: // Dân làng 
                Debug.Log("[ActionOnclickAvatar] Đã vào case 1");
                break;
            case 2 when gamePhase == "Night" && haschossenTarget == true: // Tiên tri
                ProPhetAction(victimActor,avt);
                haschossenTarget = false;
                Debug.Log("[ActionOnclickAvatar] Đã vào case 2");
                break;
            case 3 when gamePhase == "Night" && haschossenTarget == true: // Bảo vệ
                GuardianActionLocal(victimActor,avt);   
                haschossenTarget = false;
                Debug.Log("[ActionOnclickAvatar] Đã vào case 3");
                break;
            case 4 when gamePhase == "Night": // Sói
                WolfVote(victimActor);
                Debug.Log("[ActionOnclickAvatar] Đã vào case 4");
                break;
            default:
                Debug.Log("Không có action này !!!");
                break;
        }

    }
    // master sẽ dựa theo NumberOfAction để setActive skill của nhân vật // Và Khi reset master sẽ gửi lệnh tắt active hết skill button
    #region Skill
    private string skillPhase;
    public Button ProphetSkill;
    public Button GuardianSkill;
    // của sói
    public Button WolfNightChat;
    public Button WolfDayChat;

    public void ResetSkillByOnNight()
    {
        photonView.RPC("ResetGuardianProtected", RpcTarget.All);
    }
    public void ProPhetAction(int victimActor, AvatarManager avt)
    {
        if (!PlayerRoles.ContainsKey(victimActor))
        {
            Debug.LogError($"[ProPhetAction] Không tìm thấy actor {victimActor} trong PlayerRoles.");
            return;
        }
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        roleType r = PlayerRoles[victimActor];
        switch (r)
        {
            case roleType.Villager:
                avt.Skill.sprite = Resources.Load<Sprite>("Skill/prophet3");
                StartCoroutine(ShowSkillForSeconds(avt.Skill, 2f));
                Debug.Log("[ProPhetAction] thiện");
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageTarget($"<color=#00FF99><i>🔮 Linh cảm của bạn mách bảo: Người này mang ánh sáng của chính nghĩa.</i></color>",localActor);
                ptChat.SystemMessage?.Invoke();
                break;

            case roleType.Wolf:
                avt.Skill.sprite = Resources.Load<Sprite>("Skill/prophet2");
                StartCoroutine(ShowSkillForSeconds(avt.Skill, 2f));
                Debug.Log("[ProPhetAction] ác");
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageTarget($"<color=#FF3333><i>🔮 Bóng tối bao phủ... Người này mang trong mình khí chất của kẻ săn mồi.</i></color>", localActor);
                ptChat.SystemMessage?.Invoke();
                break;

            case roleType.Solo:
                avt.Skill.sprite = Resources.Load<Sprite>("Skill/prophet1");
                StartCoroutine(ShowSkillForSeconds(avt.Skill, 2f));
                Debug.Log("[ProPhetAction] không rõ");
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageTarget($"<color=#CCCCCC><i>🔮 Màn sương quá dày... Bạn không thể xác định bản chất của người này.</i></color>", localActor);
                ptChat.SystemMessage?.Invoke();
                break;

            default:
                Debug.LogError($"[ProPhetAction] Vai trò không hợp lệ: {r}");
                break;
        }
    } // xong
    #region Guardian skill
    public void GuardianActionLocal(int victimActor, AvatarManager avt)
    {
        if (!PlayerRoles.ContainsKey(victimActor))
        {
            Debug.LogError($"[ProPhetAction] Không tìm thấy actor {victimActor} trong PlayerRoles.");
            return;
        }
        avt.Skill.sprite = Resources.Load<Sprite>("Skill/GuardianSkill");
        StartCoroutine(ShowSkillForSeconds(avt.Skill, 2f));
        photonView.RPC("ClientGuardianSetProtected", RpcTarget.All, victimActor);
    }
    [PunRPC]
    public void ClientGuardianSetProtected(int victimActor)
    {
        AvatarManager avt = FindAvatarByActorNumber(victimActor);
        if(avt != null)
        {
            avt.isProtected = true;
            Debug.Log("[ClientGuardianSetProtected] đã bảo vệ người chơi  ");
        }
    }
    [PunRPC]
    public void ResetGuardianProtected()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values) 
        {
            int victimActor = player.ActorNumber;
            AvatarManager avt = FindAvatarByActorNumber(victimActor);
            if (avt != null && avt.isProtected)
            {
                avt.isProtected = false;
                Debug.Log("[ClientGuardianSetProtected] đã tắt bảo vệ người chơi  ");
            }
        }
    }
    #endregion 
    // xong Guardian skill
    private IEnumerator ShowSkillForSeconds(Image skillImage, float duration)
    {
        skillImage.gameObject.SetActive(true); // Bật hình ảnh
        yield return new WaitForSeconds(duration); // Đợi 2 giây
        skillImage.gameObject.SetActive(false); // Tắt hình ảnh
    }
    public void SetButtonSkill() // button skill sẽ gọi hàm này ở dạng local để xét điều kiện khi nhấn Action trên avt
    {
        if (ControlSkill)
        {
            haschossenTarget = true;
            Debug.Log($"[Local] haschossenTarget == {haschossenTarget} ");
            ControlSkill = false;
        }
        else
        {
            Debug.Log($"[SetButtonSkill] ControlSkill = {ControlSkill} chỉ có thể sử dụng skill 1 lần mỗi đêm");
        }
    }
    [PunRPC]
    public void SetActiveSkill() // NumberofAction = noa // chỉ gọi 1 lần đúng đầu trận do master gọi
    {
        int noa = NumberOfAction;
        switch (noa)
        {
            case 2:
                ProphetSkill.gameObject.SetActive(true);
                break;
            case 3:
                GuardianSkill.gameObject.SetActive(true); 
                break;
            case 4:
                WolfNightChat.gameObject.SetActive(true);
                WolfDayChat.gameObject.SetActive(true); 
                break;
            default:
                break;
        }
    }
    [PunRPC]
    public void ResetActiveAllSkill() // master gọi để phát reset
    {
        ProphetSkill.gameObject.SetActive(false);
        GuardianSkill.gameObject.SetActive(false);
        // sói
        WolfNightChat.gameObject.SetActive(false);
        WolfDayChat.gameObject.SetActive(false);
    }
    #endregion
    // Set time cho các action bên dưới Chỉ chia dưới dạng local
    public void villagerFT()
    {
        opCard.showcard = () => opCard.OpenVillager();
        NumberOfAction = 1;
        Debug.Log("Dân làng vô dụng");
    }
    public void ProphetFT()
    {
        opCard.showcard = () => opCard.OpenProphet();
        NumberOfAction = 2;
        Debug.Log("Hãy tiên đoán 1 người chơi");
    }
    public void GuardianFT()
    {
        opCard.showcard = () => opCard.OpenGuardian();
        NumberOfAction = 3;
        Debug.Log("Bảo vệ 1 người chơi");
    }
    public void WolfFT() // tạm để sói thường , các FT sói khác vẫn để numberOfAction là 4 chỉ cần thêm 1 dữ liệu chung để when
    {
        opCard.showcard = () => opCard.OpenNormalWolf();
        NumberOfAction = 4;
        Debug.Log("Thức dậy cắn người!!");
    }
    #region vote
    public void WolfVote(int victimActor)
    {
        if (victimActor == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("[Vote] không thể vote cắn chính mình !!!");
            return;
        }
        AvatarManager victim = FindAvatarByActorNumber(victimActor);
        if(victim != null && victim.role == roleType.Wolf)
        {
            Debug.Log("[Vote] không thể vote cắn đồng loại !!!");
            return;
        }
        if (currentvote == victimActor)
        {
            VoteCount.Remove(victimActor);
            currentvote = -1;
            ClientSendMasterClearBoardTextVote(PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC("SubmitDeleteVoteToMaster", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log($"Đã hủy vote cắn cho :{victimActor}");
        }
        else
        {
            if (currentvote != -1) // nếu đã vote ai khác trước đó xóa đi khỏi danh sách
            {
                VoteCount.Remove(currentvote);

            }
            VoteCount.Add(victimActor);
            currentvote = victimActor;

            Debug.Log($"Đã chuyển vote cắn sang cho: {victimActor}");
            // gửi vote lên master client
            photonView.RPC("SubmitVoteToMaster", RpcTarget.MasterClient, currentvote, PhotonNetwork.LocalPlayer.ActorNumber);
            PointedOnForWolf(victimActor);
            currentvote = -1;
            VoteCount.Remove(currentvote);
        }
    }
    public void Vote(int newTargetPlayerActorNumber)
    {
        if(newTargetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("[Vote] không thể vote chính mình !!!");
            return;
        }
        if( currentvote == newTargetPlayerActorNumber)
        {
            VoteCount.Remove(newTargetPlayerActorNumber);
            currentvote = -1;
            ClientSendMasterClearBoardTextVote(PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC("SubmitDeleteVoteToMaster", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log($"Đã hủy vote cho :{newTargetPlayerActorNumber}");
        }else
        {
            if(currentvote != -1) // nếu đã vote ai khác trước đó xóa đi khỏi danh sách
            {
                VoteCount.Remove(currentvote);
                photonView.RPC("SubmitDeleteVoteToMaster", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            VoteCount.Add(newTargetPlayerActorNumber);
            currentvote = newTargetPlayerActorNumber;
            
            Debug.Log($"Đã chuyển vote sang cho: {newTargetPlayerActorNumber}");
            // gửi vote lên master client
            photonView.RPC("SubmitVoteToMaster", RpcTarget.MasterClient, currentvote, PhotonNetwork.LocalPlayer.ActorNumber);
            PointedPublic(newTargetPlayerActorNumber);
            currentvote = -1;
            VoteCount.Remove(currentvote);
        }
    }
    [PunRPC]
    void SubmitVoteToMaster(int TargetPlayerActorNumber, int voterActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) { return; } // chỉ master xử lý
        if (TargetPlayerActorNumber == voterActorNumber)
        {
            Debug.Log($"[Master] Người chơi {voterActorNumber} cố vote chính mình. Bỏ qua.");
            return;
        }
        MasterVoteCount[voterActorNumber] = TargetPlayerActorNumber;
        

    }
    [PunRPC]
    void SubmitDeleteVoteToMaster(int voterActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) { return; } // chỉ master xử lý
        if (MasterVoteCount.ContainsKey(voterActorNumber))
        {
            MasterVoteCount.Remove(voterActorNumber);
            Debug.Log($"[Master] Đã hủy vote cho người chơi {voterActorNumber}");
        }


    }
    public int GetMostVote(Dictionary<int,int> votes)
    {
        var voteResult = votes.Values
                              .GroupBy(v => v)
                              .OrderByDescending(g => g.Count())
                              .FirstOrDefault();
        return voteResult?.Key ?? -1;
        
    }
    void EndVotingWolf() // hàm xử lý endVoteWolf
    {
        if(!PhotonNetwork.IsMasterClient) { return; } // chỉ master xử lý

        int mostVotePlayer = GetMostVote(MasterVoteCount);
        AvatarManager avm = FindAvatarByActorNumber(mostVotePlayer);
        if(avm != null)
        {
            if (avm.isProtected) 
            {
                MasterVoteCount.Clear();
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageAllWolf($"<color=#4B0082><i>🐺 Mục tiêu đã thoát chết. Có kẻ đã bảo vệ hắn trong đêm...</i></color>");
                foreach(Player p in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    AvatarManager guardianActor = FindAvatarByActorNumber(p.ActorNumber);
                    if (guardianActor != null && guardianActor.CardName == "Bảo Vệ")
                    {
                        ptChat.SystemMessage += () => ptChat.HandleSystemMessageTarget($"<color=#00CED1><i>🛡️ Bạn đã bảo vệ đúng người! Nạn nhân đã thoát khỏi nanh vuốt của bầy sói.</i></color>", p.ActorNumber);
                    }
                }
                

                ptChat.SystemMessage?.Invoke();
                return; 
            }
        }
        if (mostVotePlayer == -1)
        {
            Debug.Log("Không có phiếu bầu !!");
            ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#4B0082><i>🌘 Đêm qua trôi qua mà không có tiếng hét nào...</i></color>");
            ptChat.SystemMessage?.Invoke();
            return;
        }
        else
        {

            int VotesForPlayer = MasterVoteCount.Values.Count(v => v == mostVotePlayer);
            int totalWolf = 0;
            foreach(KeyValuePair<int, roleType> roles in PlayerRoles)
            {
                if(roles.Value == roleType.Wolf) { totalWolf++; }
            }

            Debug.Log($"[EndVotingWolf] WolfmostVotePlayer : {mostVotePlayer}");
            Debug.Log($"[EndVotingWolf] WolfVotesForPlayer: {VotesForPlayer}, totalWolf: {totalWolf}");
            if ((float)VotesForPlayer / totalWolf >= 0.6f)
            {
                photonView.RPC("VotingResult", RpcTarget.All, mostVotePlayer);
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#800080><i>🌒 Một linh hồn đã biến mất trong bóng tối...</i></color>");
                ptChat.SystemMessage?.Invoke();
                
            }
            else
            {
                Debug.Log($"Tổng số phiếu không đủ 60% người chơi");
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#4B0082><i>🌘 Đêm qua trôi qua mà không có tiếng hét nào...</i></color>");
                ptChat.SystemMessage += () => ptChat.HandleSystemMessageAllWolf($"<color=#4B0082><i>🐺 Không có nạn nhân nào vì phe sói không thống nhất mục tiêu.</i></color>");
                ptChat.SystemMessage?.Invoke();

            }
        }
        MasterVoteCount.Clear();
    }
    void EndVoting() // hàm xử lý Vote
    {
        if(!PhotonNetwork.IsMasterClient) { return; } // chỉ master xử lý

        int mostVotePlayer = GetMostVote(MasterVoteCount);

        if (mostVotePlayer == -1)
        {
            Debug.Log("Không có phiếu bầu !!");
            ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#AAAAAA><i>⚖️ Dân làng đã không đưa ra phán quyết nào hôm nay. Một ngày trôi qua trong im lặng...</i></color>");
          
            ptChat.SystemMessage?.Invoke();
            MasterVoteCount.Clear();
            return;
        }
        else
        {

            int VotesForPlayer = MasterVoteCount.Values.Count(v => v == mostVotePlayer);
            int totalPlayer = PhotonNetwork.PlayerList.Length - OutPlayer;

            Debug.Log($"[EndVoting] mostVotePlayer : {mostVotePlayer}");
            Debug.Log($"[EndVoting] VotesForPlayer: {VotesForPlayer}, totalPlayer: {totalPlayer}");
            if ((float)VotesForPlayer / totalPlayer >= 0.6f)
            {
                photonView.RPC("VotingResult", RpcTarget.All, mostVotePlayer);
                AvatarManager mostVoteActor = FindAvatarByActorNumber(mostVotePlayer);
                if (mostVoteActor != null)
                {
                    ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#FF4500><b>📢 Người chơi {mostVoteActor.nameText.text} đã bị xử tử trước toàn dân làng!</b></color>");
                    ptChat.SystemMessage?.Invoke();
                }
            }
            else
            {
                Debug.Log($"Tổng số phiếu không đủ 60% người chơi");
                ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#999999><i>⏳ Phiếu bầu không đạt đa số cần thiết. Một ngày nữa trôi qua trong do dự.</i></color>");

                ptChat.SystemMessage?.Invoke();
            }
        }
        MasterVoteCount.Clear();
    }
    [PunRPC]
    public void MasterSetPlayerEliminated(int mostVotedPlayer)
    {
        AvatarManager avm = FindAvatarByActorNumber(mostVotedPlayer);
        if (avm != null)
        {
            avm.isAlive = false;

            Debug.Log($"[MasterSetPlayerEliminated] Trạng thái isAlive của {mostVotedPlayer} sau khi loại = {avm.isAlive}");
        }
        if (PhotonNetwork.IsMasterClient)
        {
            if (isFullyInitilized)
            {
                CheckWinCondition();
            }
        }
        Debug.Log($"[MasterSetPlayerEliminated] Đã tắt chat của người chơi: {mostVotedPlayer}");

    }
    public void SetPlayerEliminated(int mostVotedPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("MasterSetPlayerEliminated", RpcTarget.All, mostVotedPlayer);
        }
        else
        {
            photonView.RPC("ClientRequestMasterToSetPlayerEliminated", RpcTarget.MasterClient, mostVotedPlayer);
        }
    }
    [PunRPC]
    public void ClientRequestMasterToSetPlayerEliminated(int mostVotedPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("MasterSetPlayerEliminated", RpcTarget.All, mostVotedPlayer);
        }
    }
    [PunRPC]
    public void VotingResult(int mostVotedPlayer) // chỉ là hàm xử lý isAlive và độ mờ cửa avt
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == mostVotedPlayer)
        {
            NumberOfAction = -1;
            Debug.Log($"[VotingResult] NumberOfAction : {NumberOfAction}");

            SetPlayerEliminated(mostVotedPlayer);
            FuntionSetAvtTransparency(mostVotedPlayer, 0.5f);
        }
        if (PhotonNetwork.IsMasterClient)
        {
            
            Debug.Log($"[VotingResult] OutPlayer = {OutPlayer}");
            if (PlayerRoles.Remove(mostVotedPlayer))
            {
                Debug.Log($"[VotingResult] PlayerRoles[{mostVotedPlayer}] đã remove");
            }
            else
            {
                Debug.Log($"[VotingResult] PlayerRoles[{mostVotedPlayer}] đã remove");
            }
        }
    }
    #endregion

    [PunRPC]
    public void AliveAll() // phát lại ifAlive cho tất cả người chơi Khi kết thúc game
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)

        {
            int actorNumber = player.ActorNumber;
            AvatarManager avm = FindAvatarByActorNumber(actorNumber);
            if (avm != null)
            {
                avm.isAlive = true;
            }
            photonView.RPC("SetAvtTransparency", RpcTarget.All,actorNumber, 1.0f);
        }
        if (PhotonNetwork.IsMasterClient) { photonView.RPC("reset", RpcTarget.All); }
        Debug.Log("Đã phát lại Alive == true");
        
    }

    #endregion
    #region Random RPC
    [PunRPC]
    void AssignRoles(string[] assignedRoles)
    {
        int myIndex = -1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("PlayerIndex", out object index))
        {
            myIndex = (int)index - 1;
            Debug.Log("Mình là người chơi số: " + myIndex);
        }
        if (myIndex < assignedRoles.Length)
        {
            Sprite assignedSprite = System.Array.Find(NormalVilager, s => s.name == assignedRoles[myIndex]) ??
                                    System.Array.Find(Villagers, s => s.name == assignedRoles[myIndex]) ??

                                    System.Array.Find(Wolf, s => s.name == assignedRoles[myIndex]) ??
                                    System.Array.Find(Solo, s => s.name == assignedRoles[myIndex]);
            this.Card.image.sprite = assignedSprite;
            string name = GetName(assignedSprite);
            int MyActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            AvatarManager roles = FindAvatarByActorNumber(MyActorNumber);
            if(roles != null)
            {
                roles.role = getRole(name);
                roles.CardName = name;
            }
            Debug.Log($"Người chơi {PhotonNetwork.LocalPlayer.NickName} nhận vai trò: {roles.role}");
            Debug.Log($"Người chơi {PhotonNetwork.LocalPlayer.NickName} nhận nhân vật: {name}");
            ExecuteAction(name);
            photonView.RPC("ClientSendReadyToMaster", RpcTarget.All);
        }
    }
    void Shuffle(List<Sprite> list)
    {
        for(int i = list.Count - 1; i > 0; i--)
        {
            int randIndex = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[randIndex]) = (list[randIndex], list[i]);
        }
    }
    Sprite GetWeightedRandomSprite(Sprite[] options, int[] weight)
    {
        int totalWeight = 0;
        foreach(int w in weight) totalWeight+= w;

        int randomValue = UnityEngine.Random.Range(1, totalWeight + 1);

        for(int i = 0; i < options.Length; i++)
        {
            if(randomValue <= weight[i])
                return options[i];
            randomValue -= weight[i];
        }
        return options[options.Length - 1];
    }
    #endregion
    #region EndGame

    int readyCount = 0;
    [PunRPC]
    public void ClientSendReadyToMaster()
    {
        photonView.RPC("ClientIsReady", RpcTarget.MasterClient);
    }
    [PunRPC]
    void ClientIsReady()
    {
        readyCount++;
        if(readyCount == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            isFullyInitilized =true;

            var avatars = FindObjectsByType<AvatarManager>(FindObjectsSortMode.None);
            Debug.Log($"[ClientIsReady] Tổng số AvatarManager trên Master: {avatars.Length}");
            Debug.Log($"[ClientIsReady]  readyCount : {readyCount}");
            foreach (var avm in avatars)
            {
                Debug.Log($"Processing: {avm.name}");
            }
        }
    }
    public void ClientRecevedRoleFromMaster(Dictionary<int, roleType> playerRole, Dictionary<int , string> ten) // chỉ master gọi
    {
        int n = PlayerRoles.Count * 2;
        int n2 = ten.Count * 2;
        object[] data = new object[n];
        object[] data2 = new object[n2];
        
        Debug.Log($"[Master] data có độ dài = {n} ");

        int index = 0;
        foreach (KeyValuePair<int, roleType> role in playerRole)
        {
            data[index++] = role.Key;
            data[index++] = role.Value;
        }
        int index2 = 0;
        foreach(KeyValuePair<int ,string> t in ten)
        {
            data2[index2++] = t.Key;
            data2[index2++] = t.Value;
        }
        photonView.RPC("MasterSendPlayerRoleToAll", RpcTarget.Others, data, data2);
        MasterSendPlayerRoleToAll(data,data2);
        Debug.Log("[Master] Đã gửi RPC tới tất cả client");

    }
    [PunRPC]
    public void MasterSendPlayerRoleToAll(object[] DataPlayerRole, object[] DataCardName) // Client nhận lời gọi mà thực hiện hàm này
    {
        Dictionary<int, roleType> receivedrole = new Dictionary<int, roleType>();
        Dictionary<int, string> receivedCardName = new Dictionary<int, string>();
        for (int i = 0; i < DataPlayerRole.Length - 1; i += 2)
        {
            int key = (int)DataPlayerRole[i];
            roleType value = (roleType)DataPlayerRole[i + 1];
            receivedrole[key] = value;
        }

        for (int i = 0; i < DataCardName.Length - 1; i += 2)
        {
            int key = (int)DataCardName[i];
            string value = (string)DataCardName[i + 1];
            receivedCardName[key] = value;
        }

        PlayerRoles = receivedrole;
        DicCardName = receivedCardName;
        SetSceneRoleTypeAndName(receivedrole, receivedCardName);
        foreach(KeyValuePair<int, roleType> roles in PlayerRoles)
        {
            Debug.Log($"[Client] PlayerRoles[{roles.Key}] = {roles.Value}");
        }
        Debug.Log("[Client] đã nhận được danh sách role");
    }
    public void SetSceneRoleTypeAndName(Dictionary<int , roleType> roles,Dictionary<int,string> ten)
    {
        foreach(Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            AvatarManager avm = FindAvatarByActorNumber(player.ActorNumber);
            if(avm != null)
            {
                if (roles.ContainsKey(player.ActorNumber))
                {
                    avm.role = roles[player.ActorNumber];
                    avm.CardName = ten[player.ActorNumber];
                    Debug.Log($"[SetSceneRoleType] Gán role {roles[player.ActorNumber]} cho actor {player.ActorNumber}");

                }
                else
                {
                    Debug.LogError($"ActorNumber {player.ActorNumber} không có trong PlayerRoles.");
                }
            }
        }
    }
    public void MasterRpcRoleToALL()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("SendRoleToMaster", RpcTarget.All);
    }
    [PunRPC]
    public void SendRoleToMaster() // gọi khi vừa phát role và bắt đ
    {
        var avatars = FindObjectsByType<AvatarManager>(FindObjectsSortMode.None);
        var myAvatar = avatars.FirstOrDefault(avm => avm.photonView.IsMine);
        if (myAvatar != null)
        {
            photonView.RPC("ReceiveRoleFromClient", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, myAvatar.role, myAvatar.CardName);
        }
    }
    [PunRPC]
    public void ReceiveRoleFromClient(int playerID, roleType role, string ten)
    {
        PlayerRoles[playerID] = role; // Lưu vai trò của client
        DicCardName[playerID] = ten;
        Debug.Log($"[ReceiveRoleFromClient] Master nhận vai trò: {role} từ Player {playerID}");

        var avatars = FindObjectsByType<AvatarManager>(FindObjectsSortMode.None);
        //  Nếu Master Client, tự thêm vai trò của chính nó vào danh sách
        var myAvatar = avatars.FirstOrDefault(avm => avm.photonView.IsMine);
        if (PhotonNetwork.IsMasterClient && myAvatar != null)
        {
            PlayerRoles[PhotonNetwork.LocalPlayer.ActorNumber] = myAvatar.role; // Vai trò của Master
            Debug.Log($"[ReceiveRoleFromClient] Master nhận vai trò: {role} từ Player {PhotonNetwork.LocalPlayer.ActorNumber}");
        }
        if (PlayerRoles.Count >= PhotonNetwork.CurrentRoom.PlayerCount && DicCardName.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("Đã nhận đủ vai trò từ tất cả Client, gửi danh sách đến mọi người.");
            ClientRecevedRoleFromMaster(PlayerRoles,DicCardName);
            photonView.RPC("MasterSetStatusCountFirst", RpcTarget.All);
        }

    }
    #region video win
    public RawImage rawImage;
    public VideoPlayer videoPlayer;
    [PunRPC]
    public void SetVideoWin(int index)
    {
        rawImage.gameObject.SetActive(true);
        string path = $"VideoWIn/{index}";
        VideoClip clip = Resources.Load<VideoClip>(path);
        if (clip != null)
        {
            videoPlayer.clip = clip;
            videoPlayer.Play();
        }
    }
    public void ContinueGame()
    {
        rawImage.gameObject.SetActive(false);
    }
    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
        rawImage.gameObject.SetActive(false);
    }


    #endregion
    public void OnCardClick()
    {
        opCard.showcard?.Invoke();
        statusCountText.gameObject.SetActive(true);
    }
    [PunRPC]
    public void ShareTextStatus(string text)
    {
        statusCountText.text = text;
    }
    [PunRPC]
    public void MasterSetStatusCountFirst()
    {
        int VillagerCount = 0;
        int WolfCount = 0;
        int SoloCount = 0;
        foreach (var r in PlayerRoles)
        {
           
            roleType R = PlayerRoles[r.Key];
            switch(R)
            {

                case roleType.Villager: VillagerCount++; break;
                case roleType.Wolf: WolfCount++; break;
                case roleType.Solo: SoloCount++; break;
            
            }

        }
        saveTatusCount = $"<u><b><color=blue>DÂN</color></b></u>   : {VillagerCount} <u><b><color=red>SÓI</color></b></u>    : {WolfCount} <u><b><color=#4B0082>SOLO</color></b></u>: {SoloCount}";
        statusCountText.text = saveTatusCount;
    }
    // Endregion video win
    public void CheckWinCondition()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach(KeyValuePair<int , roleType> entry in PlayerRoles)
            {
                Debug.Log($"Player : {entry.Key} có vai trò : {entry.Value}");
            }
        }
        int VillagerCount = 0;
        int WolfCount = 0;
        int SoloCount = 0;
        foreach (var role in PlayerRoles)
        {
            roleType R = PlayerRoles[role.Key];
            AvatarManager avm = FindAvatarByActorNumber(role.Key);
            
            if (!avm.isAlive) continue;
            if (avm != null && avm.isAlive)
            switch (R)
            {
                
                case roleType.Villager: VillagerCount++; break;
                case roleType.Wolf: WolfCount++; break;
                case roleType.Solo: SoloCount++; break;
                
            }
        }


        Debug.Log($"[CheckWin] VILLAGER: {VillagerCount}, WOLF: {WolfCount}, SOLO: {SoloCount}");
        saveTatusCount = $"<u><b><color=blue>DÂN</color></b></u>   : {VillagerCount} <u><b><color=red>SÓI</color></b></u>    : {WolfCount} <u><b><color=#4B0082>SOLO</color></b></u>: {SoloCount}";
        photonView.RPC("ShareTextStatus", RpcTarget.All, saveTatusCount);
        if (PhotonNetwork.IsMasterClient)
        {
            OutPlayer = PhotonNetwork.PlayerList.Length - (VillagerCount + WolfCount + SoloCount);
            if (SoloCount == 0)
            {
                if (VillagerCount <= WolfCount)
                {
                    Debug.Log("Sói thắng!!");
                    photonView.RPC("SetVideoWin", RpcTarget.All, 1);
                    photonView.RPC("StopGameForAll", RpcTarget.All);
                    // resetgame
                    photonView.RPC("AliveAll", RpcTarget.All);
                    photonView.RPC("ResetActiveAllSkill", RpcTarget.All);
                    ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#00FF99><b>🌞 Ánh sáng đã chiến thắng bóng tối! Dân làng đã tiêu diệt hết lũ sói và giành lại sự bình yên cho ngôi làng.</b></color>");
                    ptChat.SystemMessage += () => ptChat.HandleSystemMessageAll($"<color=#00FF99><b>🌞 Dân làng đã thắng.</b></color>");
                    ptChat.SystemMessage?.Invoke();

                }
                else if (WolfCount == 0)
                {
                    Debug.Log("Dân làng thắng!!");
                    photonView.RPC("SetVideoWin", RpcTarget.All, 3);
                    photonView.RPC("StopGameForAll", RpcTarget.All);
                    // resetgame
                    photonView.RPC("AliveAll", RpcTarget.All);
                    photonView.RPC("ResetActiveAllSkill", RpcTarget.All);
                    ptChat.SystemMessage = () => ptChat.HandleSystemMessageAll($"<color=#B22222><b>🌑 Bóng tối đã nuốt chửng ngôi làng... Không còn ai sống sót để kể lại câu chuyện.</b></color>");
                    ptChat.SystemMessage += () => ptChat.HandleSystemMessageAll($"<color=#B22222><b>🌑 Sói đã thắng.</b></color>");
                    ptChat.SystemMessage?.Invoke();
                }
            }
        }
    }

    [PunRPC]
    void reset()
    {
        MasterVoteCount.Clear();
        OutPlayer = 0;
        VoteCount.Clear();
        isFullyInitilized = false;
        readyCount = 0;
        PlayerRoles.Clear();
        Card.image.sprite = Resources.Load<Sprite>("Avatar/1");
        if (PhotonNetwork.IsMasterClient)
        {
            OnOffButtonStart();
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
    }

    #endregion 
    //Endregion endgame
    #region Transparency Avatar
    public void FuntionSetAvtTransparency(int Playerdead, float alpha)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("MasterSetAvtTransparency", RpcTarget.MasterClient, Playerdead, alpha);
        } else
        {
            photonView.RPC("SetAvtTransparency", RpcTarget.All, Playerdead, alpha);
        }
    }
    [PunRPC]
    public void MasterSetAvtTransparency(int playerDead, float alpha)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SetAvtTransparency", RpcTarget.All, playerDead,alpha);
        }
    }
    [PunRPC]
    public void SetAvtTransparency(int PlayerDead,float alpha)
    {
        AvatarManager avm = FindAvatarByActorNumber(PlayerDead);
        if (avm != null)
        {
            Color buttonColor = avm.MyButtonPrefab.image.color;
            buttonColor.a = alpha;
            avm.MyButtonPrefab.image.color = buttonColor;

            Color knopColor = avm.knopImage.color;
            knopColor.a = alpha;
            avm.knopImage.color = knopColor;

            Color avtColor = avm.avatarImage.color;
            avtColor.a = alpha;
            avm.avatarImage.color = avtColor;

            Color frameColor = avm.frameImage.color;
            frameColor.a = alpha;
            avm.frameImage.color = frameColor;

        }
    }
    #endregion
    
}
