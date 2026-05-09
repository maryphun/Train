using UnityEngine;
using System.Collections.Generic;

static class PlayerProfile
{
    // Data that need to be saved
    static int currentDate;
    static bool[] techUnlockStatus = new bool[10];

    // Data that don't need to be saved

    static public void Initialization()
    {
        currentDate = 0;
        System.Array.Fill(techUnlockStatus, false); // set all tech unlock status into false

    }
}
