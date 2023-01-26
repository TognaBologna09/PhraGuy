using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;

public class PauseView : View
{ 

    [SerializeField]
    private Button resumeButton;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button multiplayerButton;

    [SerializeField]
    private Button menuButton;

    [SerializeField]
    private Button exitButton;

    public override void Initialize()
    {
        resumeButton.onClick.AddListener(() => ViewManager.Instance.Show<PlayerView>());

        settingsButton.onClick.AddListener(() => ViewManager.Instance.Show<SettingsView>());

        exitButton.onClick.AddListener(() => ViewManager.Instance.Show<ExitView>());

        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }
}
