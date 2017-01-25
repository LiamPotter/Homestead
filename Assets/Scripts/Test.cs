using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
public class Test :NetworkedMonoBehavior {
    public LayerMask farmMask;
    private Grid grid;
    // Use this for initialization
    void Start () {
        grid = FindObjectOfType<Grid>();
	}
	
	// Update is called once per frame
	void Update () {
        //if (!IsOwner)
        //    return;
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Clicking");
            Ray ray = (Camera.main.ScreenPointToRay(Input.mousePosition));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, farmMask))
            {
                Node nodeToChange = grid.NodeFromWorldPoint(hit.point);
                Debug.Log("is NodeToChange Null? " + ((nodeToChange==null)?"True":"False"));
                Debug.Log("myTile Null? " + ((nodeToChange.myTile == null) ? "True" : "False"));
                nodeToChange.myTile.TillTile();
               // Debug.Log("ChangingTile");
            }
        }
	}
}
