using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
using TriTools;
using Rewired;

public class TestMover : NetworkedMonoBehavior {

    private Player thisPlayer;
    private Vector3 moveVector;

	private void Start ()
    {
        thisPlayer = ReInput.players.GetPlayer(1);
	}
	
	private void Update ()
    {
        if (!IsOwner)
            return;
        TriToolHub.CreateVector3(thisPlayer.GetAxis("Horizontal"), thisPlayer.GetAxis("Vertical"), 1, TriToolHub.AxisPlane.XY, gameObject, out moveVector);
        transform.Translate(moveVector);
	}
}
