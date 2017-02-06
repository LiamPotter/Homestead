using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldItem))]
[CanEditMultipleObjects]
public class WorldItemEditor : Editor {

    SerializedProperty nameProp;
    private WorldItem thisWorldItem;
    void OnEnable()
    {
        if (!thisWorldItem)
            thisWorldItem = (WorldItem)target;
        nameProp = serializedObject.FindProperty("itemName");
        if(thisWorldItem.thisItem == null)
        {
            thisWorldItem.InitializeInvItem();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        thisWorldItem.thisItem.Name = nameProp.stringValue;
        thisWorldItem.thisItem.ThisItemType = thisWorldItem.ItemType;
        thisWorldItem.name = thisWorldItem.itemName;
        if(thisWorldItem.thisItem.ThisItemType== InvItem.IType.Seed)
        {
            thisWorldItem.thisItem.seedProps.Species = EditorGUILayout.TextField("Seed Species", thisWorldItem.thisItem.seedProps.Species);
            thisWorldItem.thisItem.seedProps.growingModel = EditorGUILayout.ObjectField("Growing Model", thisWorldItem.thisItem.seedProps.growingModel, typeof(GameObject), false) as GameObject;
            thisWorldItem.thisItem.seedProps.grownModel = EditorGUILayout.ObjectField("Grown Model", thisWorldItem.thisItem.seedProps.grownModel, typeof(GameObject), false) as GameObject;
        }
        serializedObject.ApplyModifiedProperties();
    }
    
	
}
