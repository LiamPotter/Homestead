using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldItem))]
[CanEditMultipleObjects]
public class WorldItemEditor : Editor {


    private WorldItem thisWorldItem;
    void OnEnable()
    {
        if (!thisWorldItem)
            thisWorldItem = (WorldItem)target;
        if (thisWorldItem.thisItem == null)
        {
            thisWorldItem.InitializeInvItem();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (GUILayout.Button("Recreate InvItem"))
        {
            thisWorldItem.InitializeInvItem();
        }
        DrawDefaultInspector();
        thisWorldItem.thisItem.Name = thisWorldItem.ItemName;
        thisWorldItem.thisItem.ThisItemType = thisWorldItem.ItemType;
        thisWorldItem.name = thisWorldItem.ItemName;
        if (thisWorldItem.thisItem.ThisItemType== InvItem.IType.Seed)
        {
            thisWorldItem.seedSpecies= EditorGUILayout.TextField("Seed Species", thisWorldItem.seedSpecies);
            thisWorldItem.thisItem.seedProps.growingModel = EditorGUILayout.ObjectField("Growing Model", thisWorldItem.thisItem.seedProps.growingModel, typeof(GameObject), false) as GameObject;
            thisWorldItem.thisItem.seedProps.grownModel = EditorGUILayout.ObjectField("Grown Model", thisWorldItem.thisItem.seedProps.grownModel, typeof(GameObject), false) as GameObject;
        }
        serializedObject.ApplyModifiedProperties();
    
    }
 
	
}
