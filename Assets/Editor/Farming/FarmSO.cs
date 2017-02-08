using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Farm", menuName = "CreateFarm", order = 1)]
public class FarmSO : ScriptableObject {

   public List<Object> farms = new List<Object>();
}
