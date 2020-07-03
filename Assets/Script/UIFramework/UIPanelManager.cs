using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPanelManager : Singleton<UIPanelManager>
{
    private bool _ready = false;

    public bool ready {
        get { return _ready; }
    }

    private RectTransform panelRoot;
    private GraphicRaycaster panelRaycaster;
    private List<BasePanel> panelList = new List<BasePanel>();
    private Dictionary<string, BasePanel> panelDic;
    private BasePanel currentPanel = null;
    private BasePanel lastPanel = null;

    //管理动画
    private bool waitingAnimationFinished = false;
    private bool currentAnimationFinished = false;
    private bool lastAnimationFinished = false;
    private bool popPanel = false;
    private string mainPanel = "MainPanel";

    private float animationDuration = 0.2f;
    public UIPanelManager()
    {
        InitPanelManager();
    }

    private BasePanel LoadPanel(string panelName)
    {
        BasePanel panel = (BasePanel)Resources.Load<BasePanel>("UIPanels/"+panelName);
        if (panel==null)
        {
            Debug.LogWarning("Panel not found" + panelName);
            return null;
        }
        panel.InitializeComponet();
        //if (panel.frontLayouts!=null)
        //{

        //}
        if (panel==null)
        {
            Debug.LogWarning("Load panel prefab failed:"+ panelName);
        }
        else
        {
            panel.gameObject.SetActive(true);
        }
        return panel;
    }

    private void InitPanelManager()
    {
        panelDic = new Dictionary<string, BasePanel>();
        panelRoot = GameObject.Find("PanelRoot").GetComponent<RectTransform>();
        panelRaycaster = panelRoot.GetComponent<GraphicRaycaster>();

        _ready = true;
    }

    private BasePanel GetPanel(string panelType)
    {
        return LoadPanel(panelType);
    }
    int statechange = 0;
    public void PushPanel(string panelType ,object data =null)
    {
        if (!_ready)
        {
            Debug.Log("Not Ready");
            return;
        }

        if (waitingAnimationFinished)
        {
            Debug.LogWarning("Can 't Push Panel beacuse waitingAnimtionFinished is true");
        }

        BasePanel bp = GetPanel(panelType);
        if (bp==null)
        {
            Debug.LogWarning("panelNotFound:"+panelType);
            return;
        }
        waitingAnimationFinished = true;
        popPanel = false;

        BasePanel panel = UnityEngine.Object.Instantiate<BasePanel>(bp,new Vector3(10000f,10000f,0),Quaternion.identity,panelRoot);
        panel.transform.SetAsLastSibling();
        panel.gameObject.SetActive(true);
        panelList.Add(panel);

        //没有panel的时候 不能设置lastPanel
        if (panelList.Count>1)
        {
            lastPanel = panelList[panelList.Count - 2];
        }
        else
        {
            lastPanel = null;
        }
        //如果是mainPanel 则不能出栈，该Panel用户游戏主界面 比如阴阳师 庭院 界面
        if (panelType == mainPanel)
        {
            for (int i=panelList.Count-3;i>=0 ;i-- )
            {
                try
                {
                    panelList[i].OnUnload();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                    Debug.LogWarning(e.StackTrace);
                }
                GameObject.DestroyImmediate(panelList[i].gameObject);
                panelList.RemoveAt(i);
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        currentPanel = panel;
        try
        {
            panel.OnLoad(data);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            Debug.LogWarning(e.StackTrace);
        }
        panelRaycaster.enabled = false;
        EventSystem.current.SetSelectedGameObject(null);

        lastAnimationFinished = false;
        currentAnimationFinished = false;
        statechange = 0;

        if (lastPanel!=null)
        {
            try
            {
                lastPanel.OnBeforeExit();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                Debug.LogWarning(e.StackTrace);
            }
            PlayExitAnimation(lastPanel,panel);
        }
        else
        {
            lastAnimationFinished = true;
        }
        if (panel.managedAnimation)
        {
            try
            {
                panel.OnBeforeEnter();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }
            PlayOpenAnimation(panel,(lastPanel==null||panelType==null));
        }
        else
        {
            if (lastPanel==null)
            {
                try
                {
                    panel.StartEnterAnimation();
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    Debug.Log(e.StackTrace);
                }
            }
        }
    }

    public void PopPanel()
    {
        if (!ready) return;
        if (!currentPanel.CanPopPanel()) return;

        if (waitingAnimationFinished)
        {
            Debug.LogWarning("Can't PopPanel because waitingAnimationFinished is true");
            return;
        }

        if (panelList.Count<=1)
        {
            return;
        }
        panelRaycaster.enabled = false;
        EventSystem.current.SetSelectedGameObject(null);

        lastAnimationFinished = false;
        currentAnimationFinished = false;
        popPanel = true;
        BasePanel closingPanel = panelList[panelList.Count - 1];
        BasePanel enteringPanel = null;
        if (panelList.Count>1)
        {
            enteringPanel = panelList[panelList.Count - 2];
            enteringPanel.gameObject.SetActive(true);
            panelList.RemoveAt(panelList.Count-1);
        }

        if (enteringPanel == null) return;
        lastPanel = closingPanel;
        currentPanel = enteringPanel;

        if (enteringPanel.managedAnimation)
        {
            try
            {
                enteringPanel.OnBeforeEnter();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
               
            }
            PlayResumeAnimation(enteringPanel, closingPanel);
        }
        else
        {
            if (closingPanel.managedAnimation)
            {

            }
        }
        try
        {
            closingPanel.OnBeforeExit();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e.StackTrace);
        }
        PlayCloseAnimation(closingPanel);
    }

    //managed animations:
    //

    private void PlayOpenAnimation(BasePanel panel,bool first=false)
    {
        currentPanel = panel;
        if (panel.managedAnimation)
        {
            panel.gameObject.SetActive(true);
            RectTransform rectTrans = panel.GetComponent<RectTransform>();

            Vector2 endpos = Vector2.zero;
            if (!first)
            {
                if (panel.animationType == AnimationType.None)
                {
                    rectTrans.DOAnchorPos(endpos, 0f).OnComplete(InternalCurrentAnimationFinished).Play();
                }
                else if (panel.animationType == AnimationType.PushFromSide)
                {
                    Vector3 startpos = endpos + new Vector2(panelRoot.sizeDelta.x, 0f);
                    rectTrans.DOAnchorPos(endpos, animationDuration).ChangeStartValue(startpos).SetEase(Ease.OutQuad).OnComplete(InternalCurrentAnimationFinished).Play();
                }
                else if (panel.animationType == AnimationType.PushFromBottom)
                {
                    Vector3 startpos = new Vector3(0f, endpos.y - panelRoot.sizeDelta.y);
                    rectTrans.DOAnchorPos(endpos, animationDuration).ChangeStartValue(startpos).OnComplete(InternalCurrentAnimationFinished).Play();
                }
            }
            else
            {
                rectTrans.DOAnchorPos(endpos, 0f).OnComplete(InternalCurrentAnimationFinished).Play();
            }
        }
        else
        {
            try
            {
                panel.StartEnterAnimation();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }
        }
    }
    private void PlayCloseAnimation(BasePanel panel)
    {
        lastPanel = panel;

        if (panel.managedAnimation)
        {
            panel.gameObject.SetActive(true);

            RectTransform rectTrans = panel.GetComponent<RectTransform>();
            Vector2 oripos = Vector2.zero;
            if (panel.animationType == AnimationType.None)
            {
                rectTrans.DOAnchorPos(oripos, 0f).OnComplete(InternalLastAnimationFinished).Play();
            }
            else if (panel.animationType == AnimationType.PushFromSide)
            {
                Vector3 endpos = Vector2.zero + new Vector2(panelRoot.sizeDelta.x, 0f);
                rectTrans.DOAnchorPos(endpos, animationDuration).SetEase(Ease.OutQuad).OnComplete(InternalLastAnimationFinished).Play();
            }
            else if (panel.animationType == AnimationType.PushFromBottom)
            {
                Vector3 endpos = new Vector3(0f, oripos.y - panelRoot.sizeDelta.y);
                rectTrans.DOAnchorPos(endpos, animationDuration).OnComplete(InternalLastAnimationFinished).Play();
            }
        }
        else
        {
            try
            {
                panel.StartExitAnimation();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }
        }
    }

    private void PlayResumeAnimation(BasePanel panel,BasePanel prevPanel)
    {
        currentPanel = panel;
        if (panel.managedAnimation && prevPanel.managedAnimation)
        {
            if (panel.animationType == AnimationType.None)
            {
                InternalCurrentAnimationFinished();
            }
            else if (panel.animationType == AnimationType.PushFromSide)
            {
                RectTransform lastRectTrans = panel.GetComponent<RectTransform>();
                Vector2 lastEndPos = Vector2.zero;
                lastRectTrans.DOAnchorPos(lastEndPos, animationDuration).SetEase(Ease.OutQuad).OnComplete(InternalCurrentAnimationFinished).Play();
            }
            else if (panel.animationType == AnimationType.PushFromBottom)
            {
                InternalCurrentAnimationFinished();
            }
        }
        else
        if (panel.managedAnimation && !prevPanel.managedAnimation)
        {
            if (panel.animationType == AnimationType.None)
            {
                InternalCurrentAnimationFinished();
            }
            else if (panel.animationType == AnimationType.PushFromSide)
            {
                RectTransform lastRectTrans = panel.GetComponent<RectTransform>();
                Vector2 lastEndPos = Vector2.zero;
                lastRectTrans.DOAnchorPos(lastEndPos, animationDuration).SetEase(Ease.OutQuad).OnComplete(InternalCurrentAnimationFinished).Play();
            }
            else if (panel.animationType == AnimationType.PushFromBottom)
            {
                RectTransform lastRectTrans = panel.GetComponent<RectTransform>();
                Vector2 endpos = new Vector3(0f, 0f);
                lastRectTrans.DOAnchorPos(endpos, animationDuration).OnComplete(InternalCurrentAnimationFinished).Play();
            }
        }
        else if (!panel.managedAnimation)
        {
            // wait for exit animation played and deactivate it
            try
            {
                panel.StartEnterAnimation();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }
        }
    }
    private void PlayExitAnimation(BasePanel panel,BasePanel nextPanel)
    {
        lastPanel = panel;
        if (panel.managedAnimation && nextPanel.managedAnimation)
        {
            if (panel.animationType == AnimationType.None)
            {
                InternalLastAnimationFinished();
            }
            else if (panel.animationType == AnimationType.PushFromSide)
            {
                RectTransform lastRectTrans = panel.GetComponent<RectTransform>();
                Vector2 lastEndPos = Vector2.zero - new Vector2(panelRoot.sizeDelta.x / 2, 0f);
                lastRectTrans.DOAnchorPos(lastEndPos, animationDuration).SetEase(Ease.OutQuad).OnComplete(InternalLastAnimationFinished).Play();
            }
            else if (panel.animationType == AnimationType.PushFromBottom)
            {
                InternalLastAnimationFinished();
            }
        }
        else
        if (panel.managedAnimation && !nextPanel.managedAnimation)
        {
            if (panel.animationType == AnimationType.None)
            {
                InternalLastAnimationFinished();
            }
            else if (panel.animationType == AnimationType.PushFromSide)
            {
                RectTransform lastRectTrans = panel.GetComponent<RectTransform>();
                Vector2 lastEndPos = Vector2.zero - new Vector2(panelRoot.sizeDelta.x, 0f);
                lastRectTrans.DOAnchorPos(lastEndPos, animationDuration).SetEase(Ease.OutQuad).OnComplete(InternalLastAnimationFinished).Play();
            }
            else if (panel.animationType == AnimationType.PushFromBottom)
            {
                RectTransform lastRectTrans = panel.GetComponent<RectTransform>();
                Vector2 endpos = new Vector3(0f, 0f - panelRoot.sizeDelta.y);
                lastRectTrans.DOAnchorPos(endpos, animationDuration).OnComplete(InternalLastAnimationFinished).Play();
            }
        }
        else if (!panel.managedAnimation)
        {
            // wait for exit animation played and deactivate it
            try
            {
                panel.StartExitAnimation();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }
        }
    }
    void InternalCurrentAnimationFinished()
    {
        currentAnimationFinished = true;
        try
        {
            currentPanel.OnEnter();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e.StackTrace);
        }
        CheckAnimationFinished();
    }

    void InternalLastAnimationFinished()
    {
        lastAnimationFinished = true;
        try
        {
            lastPanel.OnExit();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e.StackTrace);
        }
        lastPanel.gameObject.SetActive(false);
        if (currentPanel != null && !currentPanel.managedAnimation)
        {
            try
            {
                currentPanel.OnBeforeEnter();
                currentPanel.StartEnterAnimation();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e.StackTrace);
            }
        }
        CheckAnimationFinished();
    }

    void CheckAnimationFinished()
    {
        waitingAnimationFinished = !(currentAnimationFinished && lastAnimationFinished);
        if (currentAnimationFinished && lastAnimationFinished)
        {
            if (popPanel)
            {
                try
                {
                    lastPanel.OnUnload();
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    Debug.Log(e.StackTrace);
                }
                GameObject.DestroyImmediate(lastPanel.gameObject);
                Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            if (statechange == 1)
            {
                //todo 一些进入某些面板的 全局事件 比如隐藏人物提升性能
                statechange = 0;
            }
            panelRaycaster.enabled = true;
            EventSystem.current.SetSelectedGameObject(null);
            panelRaycaster.enabled = true;
        }
    }
}
