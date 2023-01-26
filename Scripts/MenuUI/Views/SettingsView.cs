using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
public class SettingsView : View
{
    [SerializeField]
    private Button returnButton;

    public override void Initialize()
    {
        returnButton.onClick.AddListener(() => ViewManager.Instance.Show<MultiplayerView>());

        //settingsButton.onClick.AddListener(() => ViewManager.Instance.Show<SettingsView>());

        //exitButton.onClick.AddListener(() => ViewManager.Instance.Show<ExitView>());

        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }

}
