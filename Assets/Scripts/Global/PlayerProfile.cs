using UnityEngine;
using System.Collections.Generic;

static class PlayerProfile
{
    // Data that need to be saved
    static int currentDate;
    static Clock currentClock;
    static bool[] techUnlockStatus = new bool[(int)TechType.maxCount];
    static int money;
    static int researchPoint;
    static Sprite tokaCurrentBody;

    static TokaBodyList tokabodylist;

    static public Sprite TokaCurrentBody
    {
        get
        {
            EnsureTokaBodyList();

            if (tokaCurrentBody == null && tokabodylist != null)
            {
                tokaCurrentBody = tokabodylist.defaultSprite;
            }

            return tokaCurrentBody;
        }
        set => tokaCurrentBody = value;
    }

    // Data that don't need to be saved

    static public void Initialization()
    {
        EnsureTokaBodyList();

        currentDate = 0;
        currentClock = Clock.Morning;
        System.Array.Fill(techUnlockStatus, false); // set all tech unlock status into false
        money = 0;
        researchPoint = 0;
        tokaCurrentBody = tokabodylist != null ? tokabodylist.defaultSprite : null;
    }

    static void EnsureTokaBodyList()
    {
        if (tokabodylist == null)
        {
            tokabodylist = Resources.Load<TokaBodyList>("TokaBodyList");
        }
    }
}
