using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum AnimationType { 
    PushFromSide,
    PushFromBottom,
    None
}


public abstract class BasePanel : MonoBehaviour
{
    public FrontLayout[] frontLayouts;

    public abstract void OnLoad(object data);

    public abstract void OnBeforeEnter();

    public abstract void OnEnter();

    public abstract void OnBeforeExie();

    public abstract void OnExit();

    public abstract void OnUnload();

    //如果要使用自己的动画  把面板上的 managedAnimation = false 播放完动画后要使用 UIPanelManager.Instance.PanelFinishedAnimation() 来通知 panelmanager
    public virtual void StartEnterAnimation() { }
    public virtual void StartExitAnimation() { }

    //默认框架管理动画
    public bool managedAnimation = true;
    public AnimationType animationType;
    public void InitializeComponet()
    {
        frontLayouts = GetComponentsInChildren<FrontLayout>(true);

        FieldInfo[] fieldInfos = this.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance);
        Dictionary<string, FieldInfo> dict = new Dictionary<string, FieldInfo>();
        for (int i=0;i<fieldInfos.Length ;i++ )
        {
            System.Type fieldType = fieldInfos[i].FieldType;
            if (fieldType.IsSubclassOf(typeof(Component))||fieldType.IsSubclassOf(typeof(MonoBehaviour)))
            {
                if (fieldInfos[i].GetValue(this)==null)
                {
                    dict.Add(fieldInfos[i].Name,fieldInfos[i]);
                }
            }
        }

        if (dict.Count == 0) return;

        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        for (int i=0; i<childTransforms.Length;i++)
        {
            if (dict.ContainsKey(childTransforms[i].name))
            {
                FieldInfo f = dict[childTransforms[i].name];
                Component comp = childTransforms[i].GetComponent(f.FieldType);
                if (comp==null)
                {
                    Debug.LogWarning("Component not found"+ f.Name);
                }
                else
                {
                    f.SetValue(this,comp);
                }
            }
        }
    }
}
