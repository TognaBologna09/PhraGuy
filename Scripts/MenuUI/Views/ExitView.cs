using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
public class ExitView : View
{
    [SerializeField]
    private Button yesButton;

    [SerializeField]
    private Button noButton;

    public override void Initialize()
    {
        yesButton.onClick.AddListener(() => ViewManager.Instance.Show<TitleView>()); // TitleView --> Application.Quit();

        noButton.onClick.AddListener(() => ViewManager.Instance.Show<MainView>());


        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }

}
