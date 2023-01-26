using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
public class MainView : View
{
    [SerializeField]
    private Button playButton;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button exitButton;


    public override void Initialize()
    {
        playButton.onClick.AddListener(() => ViewManager.Instance.Show<MultiplayerView>());
       
        settingsButton.onClick.AddListener(() => ViewManager.Instance.Show<SettingsView>());

        exitButton.onClick.AddListener(() => ViewManager.Instance.Show<ExitView>());

        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }

}
