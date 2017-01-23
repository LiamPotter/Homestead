using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class AutoSceneOpener:EditorWindow
{

    public SceneOpenerVariables variables; //public reference to the ScriptableObject that holds all the relevent variables

    [MenuItem("Trilum Tools/Auto Open")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow(typeof(AutoSceneOpener));
        GetWindow(typeof(AutoSceneOpener)).minSize = new Vector2(200, 300);
        
    }
    public void OnEnable()
    {
        //if there is no ASOInstance asset, create one, else simply load the one found
        if((SceneOpenerVariables)AssetDatabase.LoadAssetAtPath
            ("Assets/Resources/ASOInstance.asset",
            typeof(SceneOpenerVariables))==null)
        {
            variables = new SceneOpenerVariables();
            AssetDatabase.CreateAsset(variables, "Assets/Resources/ASOInstance.asset");
        }
        titleContent = new GUIContent("Scene Opener");
        LoadData();
    }
    public void OnDisable()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private bool showSceneList;

    //Controls the right-click contextual menu
    [MenuItem("Assets/Open Multi Scenes")]
    static void LoadScenesFromRightClick()
    {
        //this method is static, so we need to load the variables again inside it
        SceneOpenerVariables soVars = (SceneOpenerVariables)AssetDatabase.LoadAssetAtPath
            ("Assets/Resources/ASOInstance.asset",
            typeof(SceneOpenerVariables));
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene("Assets/Scenes/"+Selection.activeObject.name+".unity",OpenSceneMode.Single);
        foreach (Object o in soVars.Auto_Open_Scenes)
        {
            if(o.name!=soVars.sceneToSwitchTo.name)
                EditorSceneManager.OpenScene("Assets/Scenes/"+o.name + ".unity", OpenSceneMode.Additive);
        }
    }
    //This method checks if the object is a SceneAsset, if it isn't the option will be greyed out.
    [MenuItem("Assets/Open Multi Scenes", true,0)]
    static bool ValidateScenesFromRightClick()
    {
        return Selection.activeObject.GetType() == typeof(SceneAsset);
    }
    SerializedObject serializedVarObj;
    SerializedProperty serProperty;
    
    void OnGUI()
    { 
        if (variables == null)
        {
            if (GUILayout.Button("Import Data"))
            {
                LoadData();
            }

            return;
        }
        showSceneList=true;

        #region Scene Amount Manipulation
        GUIContent content0 = new GUIContent(); //Plus Symbol GUIContent
        GUIContent content1 = new GUIContent(); //Minus Symbol GUIContent
        GUIContent content2 = new GUIContent(); //Options Symbol default GUIContent
        GUIContent content3 = new GUIContent(content2); //Options Symbol dark GUIContent
        GUIContent content4 = new GUIContent(); //Background GUIContent

        content0.text = "+";
        content1.text = "-";
        content2.image = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Gizmos/TriTools/optionsButton.png",typeof(Texture));
        content3.image = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Gizmos/TriTools/optionsButtonWhite.png", typeof(Texture));
        content4.image = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Gizmos/TriTools/greyBackground.png", typeof(Texture));

        content0.tooltip = "Add a scene to load.";
        content1.tooltip = "Remove the last scene.";
        content2.tooltip = "View Options.";
        content3.tooltip = "View Options.";


        GUIStyle style0 = new GUIStyle(); //Style for most things
        style0.fontSize = 13;
        GUIStyle style1 = new GUIStyle(style0); //Style for headings
        GUIStyle style2 = new GUIStyle(); //Style for Options Symbol
        if(variables.sOpenPopup.windowTheme== SceneOpenerPopup.Theme.Dark)
            GUI.Box(new Rect(-10, -10, 1000, 1000),content4.image);//background color
        

        #region Options Button Alignment      
        float minWid;
        float maxWid;
        style2.CalcMinMaxWidth(content2, out minWid, out maxWid);
        style2.fixedWidth = minWid;
        style2.alignment = TextAnchor.UpperRight;
        style2.padding.right = 1;
        style2.padding.top = 4;
        style2.normal.background = null;
        style2.hover.textColor = Color.red;
        #endregion

        style0.padding.left = 5;
        style0.padding.top = -5;
        style1.padding.left = 5;
        style1.padding.top = 5;
        style1.fontStyle = FontStyle.Bold;
        if (variables.sOpenPopup.windowTheme == SceneOpenerPopup.Theme.Dark)
        {
            style0.normal.textColor = Color.white;
            style1.normal.textColor = Color.white;
        }
        else
        {
            style0.normal.textColor = Color.black;
            style1.normal.textColor = Color.black;
        }


        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scene Amount: "+ variables.autoSceneAmount.ToString(), style1);
        if(GUILayout.Button(content0))
        {
            variables.autoSceneAmount++;
        }
        if(GUILayout.Button(content1))
        {
            variables.autoSceneAmount--;
        }
        
        if (GUILayout.Button(variables.sOpenPopup.windowTheme ==SceneOpenerPopup.Theme.Default? content2:content3, style2))
        {
            Rect buttonRect =new Rect(Screen.width+0.1f,-35f,minWid,style2.CalcHeight(content2,minWid));
            PopupWindow.Show(buttonRect, variables.sOpenPopup);
        }
  
        EditorGUILayout.EndHorizontal();
        if (variables.autoSceneAmount<=0)
        {
            variables.autoSceneAmount = 1;
        }
        #endregion

        if (serializedVarObj==null)
            serializedVarObj = new SerializedObject(variables);
        variables.currentScene = SceneManager.GetActiveScene();
        GUILayout.Label("Current Scene: " + variables.currentScene.name, style0);
        GUILayout.Space(15);
        #region Auto Load Scene Manipulation

        serializedVarObj.Update();
        if (EditorApplication.timeSinceStartup > 1f)
        {
            int difference;
            if (variables.Auto_Open_Scenes.Count <= variables.autoSceneAmount)
            {
                difference = variables.autoSceneAmount - variables.Auto_Open_Scenes.Count;
                if (difference != 0)
                {
                    variables.Auto_Open_Scenes.Add(null);
                }
            }
            else difference = 0;
            if (variables.Auto_Open_Scenes.Count >= variables.autoSceneAmount)
            {
                difference = variables.Auto_Open_Scenes.Count - variables.autoSceneAmount;
                if (difference != 0)
                {
                    variables.Auto_Open_Scenes.RemoveAt(variables.Auto_Open_Scenes.Count - 1);
                }
            }
            else difference = 0;
            EditorGUI.indentLevel++;
            ListIterator("Auto_Open_Scenes", ref showSceneList, serializedVarObj, style0,"Scenes To Open: ");
            if (difference == 0)
            {
                for (int i = 0; i < variables.Auto_Open_Scenes.Count; i++)
                {
                    if (variables.Auto_Open_Scenes[i] != null)
                    {
                        if (variables.Auto_Open_Scenes[i].GetType()!=typeof(SceneAsset))
                        {
                            variables.Auto_Open_Scenes[i] = null;
                        }
                    }
                }
            }
        }
        serializedVarObj.ApplyModifiedProperties();
        serProperty = serializedVarObj.FindProperty("LevelScene");
        #endregion
    }

    void LoadData()
    {
        variables = (SceneOpenerVariables)AssetDatabase.LoadAssetAtPath("Assets/Resources/ASOInstance.asset", typeof(SceneOpenerVariables));
    }
    
    public void ListIterator(string propertyPath,ref bool visible,SerializedObject serializedObject,GUIStyle style,string title)
    {
        SerializedProperty listProperty = serializedObject.FindProperty(propertyPath);
        visible = EditorGUILayout.Foldout(visible, title,style);
        if (visible)
        {
            EditorGUI.indentLevel++;
            //EditorGUILayout.Space();
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                EditorGUILayout.Space();
                SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(i);
                //Rect drawZone = GUILayoutUtility.GetRect(0f, 16f);
                GUIContent contentP = new GUIContent();
                GUIStyle gStyle = new GUIStyle();
                gStyle.fontSize = style.fontSize;
                contentP.text = "Scene " + i+": ";
                contentP.tooltip = "Place any scene in this field. NOTE: Will not accept anything but SceneAssets.";
                gStyle.normal.textColor = style.normal.textColor;
                float minW;
                float maxW;
                gStyle.CalcMinMaxWidth(contentP, out minW, out maxW);
                gStyle.margin.right = 0;
                gStyle.margin.left = 0;
                gStyle.margin.top = 0;
                gStyle.margin.bottom= 2;
                //EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(contentP, gStyle);
                //EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                
                bool showChildren = EditorGUILayout.PropertyField(elementProperty,GUIContent.none);
                EditorGUI.indentLevel--;
                //EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
    
}

