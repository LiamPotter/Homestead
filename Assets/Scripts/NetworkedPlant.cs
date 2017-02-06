using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BeardedManStudios.Network;

public class NetworkedPlant : NetworkedMonoBehavior
{
    [Header("Plant Variables")]
    [Space]
    [NetSync]
    public float timeElapsed;
    public enum GStage
    {
        Seed,
        Seedling,
        Flowering,
        Fruiting,
        Mature,
        Withering
    }
    [NetSync]
    public GStage CurrentGrowthStage = GStage.Seed;
    private GStage tempStage;
    public float timeNeededEachStage;
    [NetSync]
    private float currentTimeInStage;
    public bool DebugDisplay;
    public Text timeDisplay, stageDisplay;
    public SeedProperties seedProps;
    void Start()
    {
        if (IsServerOwner)
        {
            CurrentGrowthStage = GStage.Seed;
            timeElapsed = 0;
            currentTimeInStage = 0;
            tempStage = CurrentGrowthStage;
        }
    }

    protected override void UnityUpdate()
    {
        base.UnityUpdate();
        if (DebugDisplay)
        {
            timeDisplay.text = timeElapsed.ToString();
            stageDisplay.text = CurrentGrowthStage.ToString();
        }
        else
        {
            timeDisplay.enabled = false;
            stageDisplay.enabled = false;
        }
        timeElapsed += Time.deltaTime;
        if (tempStage != CurrentGrowthStage)
        {
		 //do any events in this if
            currentTimeInStage = 0;
            tempStage = CurrentGrowthStage;
        }
        currentTimeInStage += Time.deltaTime;
        CurrentGrowthStage = CalculateGrowth();
     
    }
    private GStage CalculateGrowth()
    {
        if(CurrentGrowthStage==GStage.Seed)
        {
            if (currentTimeInStage < timeNeededEachStage)
                return GStage.Seed;
            else
                return GStage.Seedling;
        }
        if (CurrentGrowthStage == GStage.Seedling)
        {
            if (currentTimeInStage < timeNeededEachStage)
                return GStage.Seedling;
            else
                return GStage.Flowering;
        }
        if (CurrentGrowthStage == GStage.Flowering)
        {
            if (currentTimeInStage < timeNeededEachStage)
                return GStage.Flowering;
            else
                return GStage.Fruiting;
        }
        if (CurrentGrowthStage == GStage.Fruiting)
        {
            if (currentTimeInStage < timeNeededEachStage)
                return GStage.Fruiting;
            else
                return GStage.Mature;
        }
        if (CurrentGrowthStage == GStage.Mature)
        {
            if (currentTimeInStage < timeNeededEachStage)
                return GStage.Mature;
            else
                return GStage.Withering;
        }
        if (CurrentGrowthStage == GStage.Withering)
        {
            if (currentTimeInStage < timeNeededEachStage)
                return GStage.Withering;
            else Destroy(gameObject);
        }
        return GStage.Withering;
    }
}
