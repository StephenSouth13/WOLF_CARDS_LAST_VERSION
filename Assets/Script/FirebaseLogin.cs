using System.Collections.Generic;

using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.IO;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
public class FirebaseLogin : MonoBehaviourPunCallbacks
{
    public TMP_InputField TK, MK;
    public TMP_Text messageError;
    public Change_panel panelController;
    public string NameUser;
    private DatabaseReference dbRef;
    private bool isLoggedIn = false;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void Login()
    {
        string acc = TK.text;
        string pass = MK.text;

        if (string.IsNullOrEmpty(acc) || string.IsNullOrEmpty(pass))
        {
            ShowError("Vui lòng nhập đầy đủ tài khoản và mật khẩu.");
            return;
        }

        dbRef.Child("Players").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                ShowError("Lỗi kết nối Database!");
                return;
            }

            DataSnapshot snapshot = task.Result;
            foreach (DataSnapshot child in snapshot.Children)
            {
                Dictionary<string, object> player = (Dictionary<string, object>)child.Value;

                string account = player["Account"].ToString();
                string password = player["Password"].ToString();
                string isOnlineStr = player["IsOnline"].ToString();



                if (account == acc && password == pass)
                {
                    if (bool.TryParse(isOnlineStr, out bool isOnline) && isOnline)
                    {
                        ShowError("Tài khoản đang đăng nhập trên thiết bị khác!");
                        return;
                    }

                    // ✅ Chỉ khi pass đúng và chưa online thì mới vào đây
                    DatabaseReference playerRef = dbRef.Child("Players").Child(child.Key);

                    // Ghi trạng thái Online
                    playerRef.Child("IsOnline").SetValueAsync(true);

                    // Cài OnDisconnect
                    DatabaseReference connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
                    connectedRef.ValueChanged += (sender, args) =>
                    {
                        if (args.DatabaseError != null) return;

                        bool isConnected = (bool)args.Snapshot.Value;
                        if (isConnected)
                        {
                            playerRef.Child("IsOnline").OnDisconnect().SetValue(false);
                        }
                    };

                    // Các bước tiếp theo:
                    NameUser = player["Name"].ToString();
                    isLoggedIn = true;
                    PhotonNetwork.NickName = NameUser;
                    PlayerPrefs.SetString("PlayerKey", child.Key);
                    PlayerPrefs.SetString("NameUser", NameUser);
                    messageError.gameObject.SetActive(false);
                    panelController.LogInMain();
                    return;
                }

            }

            if (!isLoggedIn)
            {
                ShowError("Sai tài khoản hoặc mật khẩu!");
            }
        });
    }

    public void LogOut()
    {
        if (!PlayerPrefs.HasKey("PlayerKey")) return;

        string playerKey = PlayerPrefs.GetString("PlayerKey");
        dbRef.Child("Players").Child(playerKey).Child("IsOnline").SetValueAsync(false);
        PlayerPrefs.DeleteKey("PlayerKey");
        Debug.Log("Đã đăng xuất thành công.");
    }

    public void OnApplicationQuit()
    {
        LogOut();
        ResetLoginState();
    }

    void ShowError(string msg)
    {
        messageError.gameObject.SetActive(true);
        messageError.text = msg;
        messageError.color = Color.red;
    }
    public void ResetLoginState()
    {
        TK.text = "";
        MK.text = "";
        messageError.gameObject.SetActive(false);
    }
}
