using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;

public class StorySelectView : View
{
    [SerializeField]
    private Button startLobbyButton;

    [SerializeField]
    private Button storyModifiersButton;

    [SerializeField]
    private Button pigmentsButton;

    [SerializeField]
    private Button returnButton;

    public override void Initialize()
    {
        startLobbyButton.onClick.AddListener(() =>
        {
            InstanceFinder.ServerManager.StartConnection();

            InstanceFinder.ClientManager.StartConnection();
            
            ViewManager.Instance.Show<PlayerView>();

            Cursor.lockState = CursorLockMode.Locked;

        });
    
        storyModifiersButton.onClick.AddListener(() =>
        {
            
            //ViewManager.Instance.Show<PlayerView>();

        });

        pigmentsButton.onClick.AddListener(() =>
        {

            //ViewManager.Instance.Show<PlayerView>();

        });

        returnButton.onClick.AddListener(() => ViewManager.Instance.Show<MultiplayerView>());

        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }
}
