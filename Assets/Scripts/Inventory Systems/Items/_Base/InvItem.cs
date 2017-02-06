using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InvItem : ScriptableObject
{
    public string Name;
    public enum IType
    {
        Seed,
        Tool
    };
    public IType ThisItemType;
    public SeedProperties seedProps;
    public void UseEvent()
    {

    }
}

