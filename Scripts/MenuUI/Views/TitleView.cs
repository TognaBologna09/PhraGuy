using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem;

public class TitleView : View
{

    public override void Initialize()
    {
        InputSystem.onAnyButtonPress.CallOnce(ctrl => DoAnyKey(ctrl));

        base.Initialize();

    }

    public override void Show(object args = null)
    {
        base.Show(args);
    }

    private void DoAnyKey(InputControl context)
    {
        ViewManager.Instance.Show<MainView>();
    }
}
