using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet;

public class MultiplayerView : View
{
    [SerializeField]
    private Button findLobbyButton;

    [SerializeField]
    private Button storyButton;

    [SerializeField]
    private Button sandboxButton;

    [SerializeField]
    private Button returnButton;

    public override void Initialize()
    {
        findLobbyButton.onClick.AddListener(() => InstanceFinder.ClientManager.StartConnection());

        storyButton.onClick.AddListener(() =>
        {
         
            ViewManager.Instance.Show<StorySelectView>();

        });

        sandboxButton.onClick.AddListener(() =>
        {
            InstanceFinder.ServerManager.StartConnection();

            InstanceFinder.ClientManager.StartConnection();

            ViewManager.Instance.Show<PlayerView>();

            Cursor.lockState = CursorLockMode.Locked;
        });
        
        

        returnButton.onClick.AddListener(() => ViewManager.Instance.Show<MainView>());

        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }
}
