using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class DecolourFlashScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable YesButton;
    public KMSelectable NoButton;
    public TextMesh ScreenText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private readonly string[] _colourNames = new string[] { "RED", "ORANGE", "YELLOW", "LIME", "GREEN", "JADE", "CYAN", "AZURE", "BLUE", "VIOLET", "MAGENTA", "ROSE" };
    private static readonly Color32[] _colours = new Color32[]
    {
        new Color32(255, 0, 0, 255),    // Red
        new Color32(255, 125, 0, 255),  // Orange
        new Color32(255, 255, 0, 255),  // Yellow
        new Color32(125, 255, 0, 255),  // Lime
        new Color32(0, 255, 0, 255),    // Green
        new Color32(0, 255, 125, 255),  // Jade
        new Color32(0, 255, 255, 255),  // Cyan
        new Color32(0, 125, 255, 255),  // Azure
        new Color32(0, 0, 255, 255),    // Blue
        new Color32(125, 0, 255, 255),  // Violet
        new Color32(255, 0, 255, 255),  // Magenta
        new Color32(255, 0, 125, 255)   // Rose
    };

    private int[][][] _sequence = new int[6][][]
    {
        new int[10][] {new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] },
        new int[10][] {new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] },
        new int[10][] {new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] },
        new int[10][] {new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] },
        new int[10][] {new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] },
        new int[10][] {new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] }
    };
    private Coroutine _flashSequence;
    private int _currentStage;
    private bool _isResetting;
    private bool[] _isValid = new bool[6] { true, true, true, true, true, true };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        YesButton.OnInteract += YesPress;
        NoButton.OnInteract += NoPress;

        for (int seqNum = 0; seqNum < _sequence.Length; seqNum++)
        {
            for (int seqIx = 0; seqIx < _sequence[seqNum].Length; seqIx++)
            {
                _sequence[seqNum][seqIx][0] = Rnd.Range(0, _colourNames.Length);
                _sequence[seqNum][seqIx][1] = Rnd.Range(0, _colours.Length);
            }
        }
        _flashSequence = StartCoroutine(FlashSequence());
        CheckValidity();
    }

    private bool YesPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (!_moduleSolved && !_isResetting)
        {
            if (_isValid[_currentStage])
            {
                _moduleSolved = true;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Module.HandlePass();
                StopCoroutine(_flashSequence);
                Debug.LogFormat("[Decolour Flash #{0}] Pressed Yes on Stage #{1} successfully. Module solved.", _moduleId, _currentStage + 1);
            }
            else
            {
                Debug.LogFormat("[Decolour Flash #{0}] Pressed Yes on Stage #{1} when the sequence was illegal. Strike.", _moduleId, _currentStage + 1);
                Module.HandleStrike();
            }
        }
        return false;
    }

    private bool NoPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (!_moduleSolved && !_isResetting)
        {
            if (_isValid[_currentStage])
            {
                Debug.LogFormat("[Decolour Flash #{0}] Pressed No on Stage #{1} when the sequence was legal. Strike.", _moduleId, _currentStage + 1);
                Module.HandleStrike();
            }
            else
                Debug.LogFormat("[Decolour Flash #{0}] Pressed No on Stage #{1} successfully.", _moduleId, _currentStage + 1);
            StartCoroutine(SlotReset());
        }
        return false;
    }

    private IEnumerator FlashSequence()
    {
        while (true)
        {
            for (int i = 0; i < _sequence[_currentStage].Length; i++)
            {
                ScreenText.text = _colourNames[_sequence[_currentStage][i][0]];
                ScreenText.color = _colours[_sequence[_currentStage][i][1]];
                yield return new WaitForSeconds(0.6f);
            }
            ScreenText.text = "";
            yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator SlotReset()
    {
        _isResetting = true;
        if (_flashSequence != null)
            StopCoroutine(_flashSequence);
        Audio.PlaySoundAtTransform("Pull", transform);
        ScreenText.text = "";
        yield return new WaitForSeconds(1.2f);
        Audio.PlaySoundAtTransform("Music", transform);
        for (int i = 0; i < 48; i++)
        {
            ScreenText.text = _colourNames[Rnd.Range(0, _colourNames.Length)];
            ScreenText.color = _colours[Rnd.Range(0, _colours.Length)];
            yield return new WaitForSeconds(0.125f);
        }
        ScreenText.text = "";
        _currentStage++;
        if (_currentStage >= 6)
        {
            _moduleSolved = true;
            Module.HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Debug.LogFormat("[Decolour Flash #{0}] Six sequences have been cycled. Module solved.", _moduleId);
            yield break;
        }
        yield return new WaitForSeconds(0.5f);
        _flashSequence = StartCoroutine(FlashSequence());
        _isResetting = false;
    }

    private void CheckValidity()
    {
        for (int stage = 0; stage < _sequence.Length; stage++)
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < _sequence[stage].Length; i++)
            {
                s.Append(_colourNames[_sequence[stage][i][0]]);
                s.Append("/");
                s.Append(_colourNames[_sequence[stage][i][1]]);
                if (i != _sequence[stage].Length - 1)
                    s.Append(", ");
            }
            var str = s.ToString();
            Debug.LogFormat("[Decolour Flash #{0}] Stage #{1} sequence: {2}", _moduleId, stage + 1, str);

            // There is a GREEN word in a RED colour.
            for (int i = 0; i < _sequence[stage].Length; i++)
                if (_sequence[stage][i][0] == 4 && _sequence[stage][i][1] == 0)
                {
                    _isValid[stage] = false;
                    Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. There is a GREEN word in a RED colour.", _moduleId, stage + 1);
                    goto finished;
                }

            // A prime number of pairs in this sequence have matching words and colours.
            var primeEqual = 0;
            var primes = new int[] { 2, 3, 5, 7, 11 };
            for (int i = 0; i < _sequence[stage].Length; i++)
                if (_sequence[stage][i][0] == _sequence[stage][i][1])
                    primeEqual++;
            if (primes.Contains(primeEqual))
            {
                _isValid[stage] = false;
                Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. A prime number of pairs in this sequence have matching words and colours.", _moduleId, stage + 1);
                goto finished;
            }

            // A MAGENTA word is followed by a RED, GREEN, or BLUE, colour.
            var rgbs = new int[] { 0, 4, 8 };
            for (int i = 0; i < _sequence[stage].Length - 1; i++)
                if (_sequence[stage][i][0] == 10 && rgbs.Contains(_sequence[stage][i + 1][1]))
                {
                    _isValid[stage] = false;
                    Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. A MAGENTA word is followed by a RED, GREEN, or BLUE, colour.", _moduleId, stage + 1);
                    goto finished;
                }

            // Any four pairs in a row contain the same colour as its word or colour, unless this colour is not present anywhere in the previous sequence.
            for (int i = 0; i < _sequence[stage].Length - 3; i++)
                for (int c = 0; c < _colours.Length; c++)
                    if ((_sequence[stage][i][0] == c || _sequence[stage][i][1] == c) && (_sequence[stage][i + 1][0] == c || _sequence[stage][i + 1][1] == c) && (_sequence[stage][i + 2][0] == c || _sequence[stage][i + 2][1] == c) && (_sequence[stage][i + 3][0] == c || _sequence[stage][i + 3][1] == c))
                        for (int prev = 0; prev < i; prev++)
                            if (!_sequence[stage][prev].Contains(c))
                            {
                                _isValid[stage] = false;
                                Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. Any four pairs in a row contain the same colour as its word or colour, unless this colour is not present anywhere in the previous sequence.", _moduleId, stage + 1);
                                goto finished;
                            }

            // A YELLOW word has a VIOLET word before it in the sequence.
            for (int i = 1; i < _sequence[stage].Length; i++)
                if (_sequence[stage][i][0] == 2 && _sequence[stage][i - 1][0] == 9)
                {
                    _isValid[stage] = false;
                    Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. A YELLOW word has a VIOLET word before or after it in the sequence.", _moduleId, stage + 1);
                    goto finished;
                }

            // A CYAN word is present in the first pair of the sequence, unless the previous stage had an ORANGE colour.
            var hasCyanWord = false;
            for (int i = 0; i < 3; i++)
                if (_sequence[stage][0][0] == 6)
                    hasCyanWord = true;
            var hasOrangeColor = false;
            if (hasCyanWord && stage != 0)
                for (int i = 0; i < _sequence[stage - 1].Length; i++)
                    if (_sequence[stage - 1][i][1] == 1)
                        hasOrangeColor = true;
            if (!hasOrangeColor && hasCyanWord)
            {
                _isValid[stage] = false;
                Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. A CYAN word is present in the first three pairs of sequence, unless the previous stage had an ORANGE colour.", _moduleId, stage + 1);
                goto finished;
            }

            // Four words in a row do not contain a blue component, or five colours in a row do not contain a blue component.
            var blueComps = new int[] { 5, 6, 7, 8, 9, 10, 11 };
            for (int i = 0; i < _sequence[stage].Length - 3; i++)
            {
                if (!blueComps.Contains(_sequence[stage][i][0]) && !blueComps.Contains(_sequence[stage][i + 1][0]) && !blueComps.Contains(_sequence[stage][i + 2][0]) && !blueComps.Contains(_sequence[stage][i + 3][0]))
                {
                    _isValid[stage] = false;
                    Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. Four words in a row do not contain a blue component, or five colours in a row do not contain a blue component.", _moduleId, stage + 1);
                    goto finished;
                }
            }
            for (int i = 0; i < _sequence[stage].Length - 4; i++)
            {
                if (!blueComps.Contains(_sequence[stage][i][1]) && !blueComps.Contains(_sequence[stage][i + 1][1]) && !blueComps.Contains(_sequence[stage][i + 2][1]) && !blueComps.Contains(_sequence[stage][i + 3][1]) && !blueComps.Contains(_sequence[stage][i + 4][1]))
                {
                    _isValid[stage] = false;
                    Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. Four words in a row do not contain a blue component, or five colours in a row do not contain a blue component.", _moduleId, stage + 1);
                    goto finished;
                }
            }

            // There is a VIOLET word in either a JADE colour or ORANGE colour.
            for (int i = 0; i < _sequence[stage].Length; i++)
                if (_sequence[stage][i][0] == 9 && (_sequence[stage][i][1] == 1 || _sequence[stage][i][1] == 5))
                {
                    _isValid[stage] = false;
                    Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. There is a VIOLET word in either a JADE colour or ORANGE colour.", _moduleId, stage + 1);
                    goto finished;
                }

            // An AZURE word is present in the first or second pairs of the sequence, unless a YELLOW colour is present in the sequence.
            var hasYellowColor = false;
            for (int i = 0; i < 2; i++)
            {
                if (_sequence[stage][i][0] == 7)
                {
                    for (int j = 0; j < _sequence[stage].Length; j++)
                        if (_sequence[stage][j][1] == 2)
                            hasYellowColor = true;
                    if (!hasYellowColor)
                    {
                        _isValid[stage] = false;
                        Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. An AZURE word is present in the first or second pairs of the sequence, unless a YELLOW colour is present in the sequence..", _moduleId, stage + 1);
                        goto finished;
                    }
                }
            }

            // There is exactly one LIME colour or word and exactly one ORANGE colour or word.
            var limeCount2 = 0;
            var orangeCount = 0;
            for (int i = 0; i < _sequence[stage].Length; i++)
            {
                if (_sequence[stage][i].Contains(3))
                    limeCount2++;
                if (_sequence[stage][i].Contains(1))
                    orangeCount++;
            }
            if (limeCount2 == 1 || orangeCount == 1)
            {
                _isValid[stage] = false;
                Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. There is exactly one LIME colour or word and exactly one ORANGE colour or word.", _moduleId, stage + 1);
                goto finished;
            }

            // The first word is equal to the last colour, or the first colour is equal to the last word, unless either of them are ROSE.
            if ((_sequence[stage][0][0] == _sequence[stage][9][1] && _sequence[stage][0][0] != 11) || (_sequence[stage][0][1] == _sequence[stage][9][0] && _sequence[stage][0][0] != 11))
            {
                _isValid[stage] = false;
                Debug.LogFormat("[Decolour Flash #{0}] Stage #{1}. The first word is equal to the last colour, or the first colour is equal to the last word, unless either of them are ROSE.", _moduleId, stage + 1);
                goto finished;
            }

            Debug.LogFormat("[Decolour Flash #{0}] Stage #{1} is in a LEGAL state.", _moduleId, stage + 1);
        finished:;
        }
    }
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} press yes | !{0} press no";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        m = Regex.Match(command, @"^\s*yes\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            if (_isResetting)
            {
                yield return "sendtochaterror You can't press a button while the sequence is resetting!";
                yield break;
            }
            YesButton.OnInteract();
            yield break;
        }
        m = Regex.Match(command, @"^\s*no\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            if (_isResetting)
            {
                yield return "sendtochaterror You can't press a button while the sequence is resetting!";
                yield break;
            }
            NoButton.OnInteract();
            yield break;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = _currentStage; i < 6; i++)
        {
            if (_isValid[i])
                YesButton.OnInteract();
            else
                NoButton.OnInteract();
            while (_isResetting)
                yield return true;
        }
    }
}
