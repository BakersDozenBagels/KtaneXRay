using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRay;

public abstract class XRayModuleBase : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public GameObject[] ScanLights;
    public KMSelectable[] Buttons;
    public Material[] ScannerColors;

    protected bool _isSolved = false;

    void Start()
    {
        for (int i = 0; i < Buttons.Length; i++)
            setButtonHandler(i);
        StartModule();
    }

    protected abstract void StartModule();

    protected void MarkSolved()
    {
        Audio.PlaySoundAtTransform("X-RaySolve", transform);
        Module.HandlePass();
        _isSolved = true;
    }

    protected abstract void handleButton(int i);

    private void setButtonHandler(int i)
    {
        Buttons[i].OnInteract = delegate
        {
            Buttons[i].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[i].transform);

            if (!_isSolved)
                handleButton(i);
            return false;
        };
    }

    protected static readonly Dictionary<string, int> _twitchButtonMap = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
    {
        { "tl", 1 }, { "t", 2 }, { "tm", 2 }, { "tc", 2 }, { "tr", 2 }, { "bl", 3 }, { "b", 4 }, { "bm", 4 }, { "bc", 4 }, { "br", 5 }
    };
}
