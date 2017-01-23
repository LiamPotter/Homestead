using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneOpenerVariables : ScriptableObject
{
    public SceneOpenerPopup sOpenPopup=new SceneOpenerPopup();
    public Scene currentScene;
    public Scene sceneToSwitchTo;

    public List<Object> Auto_Open_Scenes = new List<Object>();
    public int autoSceneAmount = 1;
    public string LevelName;

    public Color windowTint;
    
    private Object _levelScene;
    public Object LevelScene
    {
        get { return _levelScene; }
        set
        {
            //Only set when the value is changed
            if (_levelScene != value && value != null)
            {
                string name = value.ToString();
                if (name.Contains(" (UnityEngine.SceneAsset)"))
                {
                    _levelScene = value;
                    LevelName = name.Substring(0, name.IndexOf(" (UnityEngine.SceneAsset)"));
                }
            }
        }
    }
}
