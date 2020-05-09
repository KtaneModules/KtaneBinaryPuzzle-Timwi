using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BinaryPuzzle;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Binary Puzzle
/// Created by Timwi
/// </summary>
public class BinaryPuzzleModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable Reset;
    public Material Gray, Green, Red;
    public TextMesh ResetLabel;

    public TextMesh[] TextMeshes;
    public MeshRenderer[] MeshRenderers;
    public KMSelectable[] Selectables;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private const int _size = 6;
    private bool[] _given;
    private bool[] _solution;
    private bool?[] _state;
    private bool _isSolved;
    private bool _resetGimmick;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;
        _resetGimmick = Rnd.Range(0, 2) != 0;

        _solution = generatePuzzle(new bool[_size * _size], new bool[_size * _size], 0).First();
        _given = new bool[_size * _size];

        var puzzleIxs = Ut.ReduceRequiredSet(Enumerable.Range(0, _solution.Length).ToArray().Shuffle(), test =>
        {
            for (int ix = 0; ix < _size * _size; ix++)
                _given[ix] = false;
            foreach (var ix in test.SetToTest)
                _given[ix] = true;
            return generatePuzzle(_solution.ToArray(), _given, 0).Take(2).Count() == 1;
        });
        for (int ix = 0; ix < _size * _size; ix++)
            _given[ix] = false;
        foreach (var ix in puzzleIxs)
            _given[ix] = true;
        reset();

        Debug.LogFormat("[Binary Puzzle #{0}] Puzzle: {1}", _moduleId, _state.Select(b => b == null ? "?" : b.Value ? "1" : "0").JoinString());
        Debug.LogFormat("[Binary Puzzle #{0}] Solution: {1}", _moduleId, _solution.Select(b => b ? "1" : "0").JoinString());

        for (int i = 0; i < _size * _size; i++)
            Selectables[i].OnInteract = pressed(i);
        Reset.OnInteract = delegate
        {
            Debug.LogFormat("[Binary Puzzle #{0}] Reset.", _moduleId);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Reset.transform);
            Reset.AddInteractionPunch();
            if (!_isSolved)
                reset();
            return false;
        };
    }

    private void reset()
    {
        _state = Enumerable.Range(0, _size * _size).Select(ix => _given[ix] ? _solution[ix] : (bool?) null).ToArray();
        _resetGimmick = !_resetGimmick;
        updateVisuals();
    }

    private KMSelectable.OnInteractHandler pressed(int i)
    {
        return delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Selectables[i].transform);
            Selectables[i].AddInteractionPunch(.1f);
            if (_isSolved || _given[i])
                return false;
            _state[i] = _state[i] == null ? false : _state[i] == false ? true : (bool?) null;
            updateVisuals();

            if (Enumerable.Range(0, _size * _size).All(ix => _state[ix] == _solution[ix]))
            {
                Debug.LogFormat(@"[Binary Puzzle #{0}] Module solved.", _moduleId);
                Module.HandlePass();
                _isSolved = true;
                ResetLabel.text = _resetGimmick ? "c o r r e c t" : "C O R R E C T";
                Reset.GetComponent<MeshRenderer>().sharedMaterial = Green;
            }

            return false;
        };
    }

    private void updateVisuals()
    {
        for (int i = 0; i < _size * _size; i++)
        {
            TextMeshes[i].gameObject.SetActive(_state[i].HasValue);
            if (_state[i].HasValue)
                TextMeshes[i].text = _state[i].Value ? "!" : "0";
            MeshRenderers[i].sharedMaterial = _state[i].HasValue ? _state[i].Value ? Green : Red : Gray;
        }
        ResetLabel.text = _resetGimmick ? "R E S E T" : "r e s e t";
    }

    private IEnumerable<bool[]> generatePuzzle(bool[] current, bool[] given, int ix)
    {
        var x = ix % _size;
        var y = ix / _size;

        if (ix == _size * _size)
        {
            yield return current;
            yield break;
        }

        var valid = new List<bool> { false, true };

        // Check that we don’t get more than two of the same digit in a straight row/column
        if (x >= 2 && current[ix - 2] == current[ix - 1])
            valid.Remove(current[ix - 1]);
        if (y >= 2 && current[ix - _size] == current[ix - 2 * _size])
            valid.Remove(current[ix - _size]);

        // Check if the current row or column already contains enough 0’s or 1’s
        var zeros = Enumerable.Range(0, x).Count(c => !current[c + _size * y]);
        if (zeros >= _size / 2)
            valid.Remove(false);
        else if (x - zeros >= _size / 2)
            valid.Remove(true);
        zeros = Enumerable.Range(0, y).Count(r => !current[x + _size * r]);
        if (zeros == _size / 2)
            valid.Remove(false);
        if (y - zeros == _size / 2)
            valid.Remove(true);

        // Make sure that the row we just filled isn’t identical to an earlier row. We can check this one column early because the last digit is determined by the rest
        if (x == _size - 2)
            for (int r = 0; r < y; r++)
                if (Enumerable.Range(0, _size - 2).All(c => current[c + _size * r] == current[c + _size * y]))
                    valid.Remove(current[_size - 2 + _size * r]);

        // Make sure that the column we just filled isn’t identical to an earlier column. We can check this one row early because the last digit is determined by the rest
        if (y == _size - 2)
            for (int c = 0; c < x; c++)
                if (Enumerable.Range(0, _size - 2).All(r => current[c + _size * r] == current[x + _size * r]))
                    valid.Remove(current[c + _size * (_size - 2)]);

        if (valid.Count > 1 && Rnd.Range(0, 2) == 0)
            for (int i = 0; i < valid.Count; i++)
                valid[i] = !valid[i];

        for (int i = 0; i < valid.Count; i++)
            if (!given[ix] || valid[i] == current[ix])
            {
                current[ix] = valid[i];
                foreach (var result in generatePuzzle(current, given, ix + 1))
                    yield return result;
            }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} A1 [toggle a square] | !{0} row 4 011001 [change an entire row] | !{0} col C 101001 [change an entire column] | !{0} solve 100101001011010110110100101001011010 [give a full solution] | !{0} reset";
#pragma warning restore 414

    private static readonly bool?[] _btnArr = new bool?[] { null, false, true };
    private KMSelectable[] ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new[] { Reset };

        Match m;
        if ((m = Regex.Match(command, @"^\s*([A-F1-6,;\s]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var arr = m.Groups[1].Value.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<KMSelectable>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (!(m = Regex.Match(arr[i], @"^([A-F])([1-6])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
                    return null;
                list.Add(Selectables[char.ToUpperInvariant(m.Groups[1].Value[0]) - 'A' + _size * (m.Groups[2].Value[0] - '1')]);
            }
            return list.ToArray();
        }

        if ((m = Regex.Match(command, @"^\s*row\s+(\d)\s+([01]{6})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var row = int.Parse(m.Groups[1].Value) - 1;
            return row < 0 || row > 5
                ? null
                : Enumerable.Range(0, 6).SelectMany(col => Enumerable.Repeat(Selectables[col + _size * row], (Array.IndexOf(_btnArr, m.Groups[2].Value[col] == '1') + 3 - Array.IndexOf(_btnArr, _state[col + _size * row])) % 3)).ToArray();
        }

        if ((m = Regex.Match(command, @"^\s*col(?:umn)?\s+([A-F]+)\s+([01]{6})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var col = char.ToUpperInvariant(m.Groups[1].Value[0]) - 'A';
            return col < 0 || col > 5
                ? null
                : Enumerable.Range(0, 6).SelectMany(row => Enumerable.Repeat(Selectables[col + _size * row], (Array.IndexOf(_btnArr, m.Groups[2].Value[row] == '1') + 3 - Array.IndexOf(_btnArr, _state[col + _size * row])) % 3)).ToArray();
        }

        return (m = Regex.Match(command, @"^\s*sol(?:ution|ve)?\s+([01]{36})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success
            ? Enumerable.Range(0, 6).SelectMany(row => Enumerable.Range(0, 6).SelectMany(col => Enumerable.Repeat(Selectables[col + _size * row], (Array.IndexOf(_btnArr, m.Groups[1].Value[col + _size * row] == '1') + 3 - Array.IndexOf(_btnArr, _state[col + _size * row])) % 3))).ToArray()
            : null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (var i = 0; i < _solution.Length; i++)
        {
            while (_state[i] != _solution[i])
            {
                Selectables[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
