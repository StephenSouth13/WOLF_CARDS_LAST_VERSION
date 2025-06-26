using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;

public class FirebaseRegister : MonoBehaviour
{
    public TMP_InputField TK, MK, NLMK, Name;
    public TMP_Text messageError, messageError2;

    DatabaseReference dbRef;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void Register()
    {
        if (string.IsNullOrEmpty(TK.text) || string.IsNullOrEmpty(MK.text) || string.IsNullOrEmpty(NLMK.text))
        {
            ShowError("Hãy nhập đầy đủ thông tin!");
            return;
        }

        if (MK.text != NLMK.text)
        {
            ShowError("Mật khẩu không khớp!");
            return;
        }

        if (string.IsNullOrEmpty(Name.text))
        {
            ShowError2("Bạn chưa đặt tên ingame!");
            return;
        }

        string acc = TK.text;
        string name = Name.text;

        dbRef.Child("Players").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                ShowError("Lỗi khi đọc dữ liệu!");
                return;
            }

            DataSnapshot snapshot = task.Result;

            // Kiểm tra trùng Account và Name
            foreach (DataSnapshot child in snapshot.Children)
            {
                Dictionary<string, object> player = (Dictionary<string, object>)child.Value;

                if (player["Account"].ToString() == acc)
                {
                    ShowError("Tài khoản đã tồn tại!");
                    return;
                }

                if (player["Name"].ToString() == name)
                {
                    ShowError2("Tên account đã tồn tại!");
                    return;
                }
            }

            // Tính ID mới
            int newId = (int)snapshot.ChildrenCount + 1;

            Players newPlayer = new Players
            {
                Account = acc,
                Password = MK.text,
                Name = name,
                AvatarPath = "Avatar/1",
                FramePath = "Frame/1",
                Id = newId,
                IsOnline = false
            };

            string json = JsonUtility.ToJson(newPlayer);
            dbRef.Child("Players").Child(acc).SetRawJsonValueAsync(json).ContinueWithOnMainThread(t =>
            {
                if (t.IsCompleted)
                    ShowSuccess("Tạo tài khoản thành công!");
            });
        });
    }


    void ShowError(string msg)
    {
        messageError.gameObject.SetActive(true);
        messageError.text = msg;
        messageError.color = Color.red;
    }

    void ShowError2(string msg)
    {
        messageError2.gameObject.SetActive(true);
        messageError2.text = msg;
        messageError2.color = Color.red;
    }

    void ShowSuccess(string msg)
    {
        messageError.gameObject.SetActive(false);
        messageError2.gameObject.SetActive(true);
        messageError2.text = msg;
        messageError2.color = Color.blue;
    }
}
