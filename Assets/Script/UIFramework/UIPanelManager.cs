using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelManager : Singleton<UIPanelManager>
{
    private bool _ready = false;

    public bool ready {
        get { return _ready; }
    }

}
