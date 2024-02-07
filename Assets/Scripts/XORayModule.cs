using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = UnityEngine.Random;

/// <summary>
/// On the Subject of XO-Rays
/// Created by BakersDozenBagels & Timwi
/// </summary>
public class XORayModule : XRayModuleBase
{
    public Renderer Strip;

    private Material _stripMaterial;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _answer1, _answer2;
    private bool _pressed1, _pressed2;
    private Vector4 _symbolCenters;

    private static readonly int[][] s_fiveChooseThree = Enumerable
        .Range(0, 5)
        .SelectMany(x => Enumerable
            .Range(x + 1, 5 - x - 1)
            .SelectMany(y => Enumerable
                .Range(y + 1, 5 - y - 1)
                .Select(z => new int[] { x, y, z })
            )
        ).ToArray();

    protected override void StartModule()
    {
        _moduleId = _moduleIdCounter++;
        _stripMaterial = Strip.material;
        GeneratePuzzle(RuleSeedable.GetRNG());
    }

    private void GeneratePuzzle(MonoRandom rng)
    {
        Log("Using ruleseed {0}.", rng.Seed);

        var icons = Enumerable
            .Range(0, 16)
            .OrderBy(x => rng.NextDouble())
            .Take(10)
            .ToArray();

        var symbol1 = Random.Range(0, 10);
        var symbol2 = Enumerable
            .Range(0, 10)
            .Except(new int[] { symbol1 })
            .Where(s => s_fiveChooseThree[symbol1].Concat(s_fiveChooseThree[s]).Distinct().Count() == 4)
            .PickRandom();

        var sets = s_fiveChooseThree[symbol1].Concat(s_fiveChooseThree[symbol2]).ToArray();

        var answers = Enumerable.Range(0, 5).Where(i => sets.Count(j => j == i) == 1).ToArray();
        _answer1 = answers[0];
        _answer2 = answers[1];

        Log("Chose symbols {0} ({2}) and {1} ({3}).", symbol1 + 1, symbol2 + 1, s_fiveChooseThree[symbol1].Select(x => x + 1).Join(" "), s_fiveChooseThree[symbol2].Select(x => x + 1).Join(" "));
        Log("Press buttons {0} and {1}.", _answer1 + 1, _answer2 + 1);

        _symbolCenters = new Vector4(
                ((icons[symbol1] % 4) + 0.5f) / 4f,
                1f - (((icons[symbol1] / 4) + 0.5f) / 4f),
                ((icons[symbol2] % 4) + 0.5f) / 4f,
                1f - (((icons[symbol2] / 4) + 0.5f) / 4f)
            );

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        const float Duration = 4f;
        float time = Duration;
        while (true)
        {
            time += Time.deltaTime;
            if (time >= Duration)
            {
                const float PiOverTwo = 0.5f * Mathf.PI;

                var angle1 = Random.Range(0,4) * PiOverTwo;
                var angle2 = Random.Range(0,4) * PiOverTwo;

                const float Scale = 1.4142135623730950488016887242097f / 6f;

                Matrix4x4 matrix = Matrix4x4.zero;

                matrix[0, 0] = +Mathf.Cos(angle1) * Scale;
                matrix[0, 1] = -Mathf.Sin(angle1) * Scale;
                matrix[1, 0] = +Mathf.Sin(angle1) * Scale;
                matrix[1, 1] = +Mathf.Cos(angle1) * Scale;

                matrix[2, 0] = +Mathf.Cos(angle2) * Scale;
                matrix[2, 1] = -Mathf.Sin(angle2) * Scale;
                matrix[3, 0] = +Mathf.Sin(angle2) * Scale;
                matrix[3, 1] = +Mathf.Cos(angle2) * Scale;

                matrix[0, 2] = _symbolCenters[0] - Mathf.Cos(angle1) * Scale / 2 + Mathf.Sin(angle1) * Scale / 2;
                matrix[1, 2] = _symbolCenters[1] - Mathf.Cos(angle1) * Scale / 2 - Mathf.Sin(angle1) * Scale / 2;
                matrix[2, 2] = _symbolCenters[2] - Mathf.Cos(angle2) * Scale / 2 + Mathf.Sin(angle2) * Scale / 2;
                matrix[3, 2] = _symbolCenters[3] - Mathf.Cos(angle2) * Scale / 2 - Mathf.Sin(angle2) * Scale / 2;

                _stripMaterial.SetMatrix("_matrix", matrix);

                time %= Duration;
            }

            _stripMaterial.SetFloat("_progress", time / Duration);

            yield return null;
            if (_isSolved) break;
        }

        _stripMaterial.SetFloat("_progress", 0f);
    }

    protected override void handleButton(int i)
    {
        if (i == _answer1)
            _pressed1 = true;
        else if (i == _answer2)
            _pressed2 = true;
        else
        {
            Log("You pressed {0}, which is incorrect. Strike!", i + 1);
            Module.HandleStrike();
            return;
        }

        Log("You pressed {0} correctly.", i + 1);
        if (_pressed1 && _pressed2)
        {
            Log("module solved.");
            MarkSolved();
        }
    }

    private void Log(object message, params object[] args)
    {
        Debug.LogFormat("[XO-Ray #" + _moduleId + "] " + message.ToString(), args);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press 3 [reading order] | !{0} press BL [buttons are TL, T, BL, B, BR]";
#pragma warning restore 414

    private static readonly string _buttonsAlternation = _twitchButtonMap.Keys.Concat(_twitchButtonMap.Values.Select(v => v.ToString())).Join("|");
    private static readonly Regex _commandMatcher = new Regex(@"^\s*(?:press\s+)?(" + _buttonsAlternation + @")(?:\s+(" + _buttonsAlternation + @"))?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | (RegexOptions)8);

    public KMSelectable[] ProcessTwitchCommand(string command)
    {
        var m = _commandMatcher.Match(command);

        if (!m.Success)
            return null;

        var buttonInput = m.Groups[1].Value;
        var buttonInput2 = m.Groups[2].Value;

        int buttonId, buttonId2;
        if ((int.TryParse(buttonInput, out buttonId) || _twitchButtonMap.TryGetValue(buttonInput, out buttonId)) && buttonId > 0 && buttonId <= Buttons.Length)
        {
            if (m.Groups[2].Success)
            {
                if ((int.TryParse(buttonInput2, out buttonId2) || _twitchButtonMap.TryGetValue(buttonInput2, out buttonId2)) && buttonId2 > 0 && buttonId2 <= Buttons.Length)
                    return new[] { Buttons[buttonId - 1], Buttons[buttonId2 - 1] };
                else
                    return null;
            }
            return new[] { Buttons[buttonId - 1] };
        }
        return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!_pressed1)
        {
            Buttons[_answer1].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        if (!_pressed2)
        {
            Buttons[_answer2].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
