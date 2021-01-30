using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ModuleLoader : MonoBehaviour
{
    [Header("Map generation")]

    [Tooltip("A \"button\" that generates a new seed.")]
    public bool randomSeed;
    [Tooltip("A number, specifying how Random should randomize.")]
    public int seed;

    [Tooltip("The limit of how far the generator can traverse\n(max amount of Passages before hitting a dead end from the spawn).")]
    [Range(1, 20)]
    public int maxDepth;

    [Header("Other options")]
    [Tooltip("A list of all modules, which can be used for map generation.\nPlease don't forget to add modules here.\nTIP: To add multiple modules, lock the Inspector.")]
    public GameObject[] possibleModules;

    private List<GameObject> map = new List<GameObject>();

    void Start()
    {
        if (randomSeed) seed = Random.Range(int.MinValue, int.MaxValue);
        if (!Application.isPlaying) return;

        Random.InitState(seed);

        for (int i = 0; i < possibleModules.Length; i++)
        {
            possibleModules[i] = Instantiate(possibleModules[i]);
            possibleModules[i].SetActive(false);
            possibleModules[i].AddComponent<ModuleObject>().SolvePassages();
        }

        int startingIndex = Random.Range(0, possibleModules.Length - 1);
        MapBranch(startingIndex, 1).SetActive(true);

        foreach (var module in map)
            foreach (var passage in module.transform.Cast<Transform>().Where(c => c.CompareTag("Passage")))
                passage.GetComponent<PassageScript>().Link(map);
    }

    int InvertPassage(int number)
    {
        return number >= 2 ? number - 2 : number + 2;
    }

    GameObject MapBranch(int moduleID, int depth, int skipR = -1)
    {
        var module = Instantiate(possibleModules[moduleID], transform);
        module.GetComponent<ModuleObject>().Reload().ClonePassages();

        int moduleIndex = map.Count;
        map.Add(module);

        //if (map.Count > 1)
        //{
        //    var dead = map[0].GetComponent<ModuleObject>();
        //    var daeod = map[1].GetComponent<ModuleObject>();
        //}

        module.SetActive(false);
        var moduleScript = module.GetComponent<ModuleObject>();

        Debug.Log(ReferenceEquals(moduleScript.passages[0], possibleModules[moduleID].GetComponent<ModuleObject>().passages[0]));

        if (depth > maxDepth)
        {
            for (int r = 0; r < 4; r++)
                foreach (var passage in moduleScript.passages[r])
                    passage.Reload();
            return module;
        }

        // For some reason combining these two loops makes Unity completely freeze.

        for (int r = 0; r < 4; r++)
        {
            var passages = new List<PassageScript>(moduleScript.passages[r]);
            foreach (var passage in passages)
            {
                passage.Reload();
                if (r == skipR)
                {
                    skipR = -1;
                    continue;
                }
                if (depth == maxDepth)
                {
                    GameObject[] possibilities = possibleModules.Where(
                        m => m.GetComponent<ModuleObject>().passages[InvertPassage(r)].Count > 0
                        && m.GetComponent<ModuleObject>().totalPassages == 1
                        ).ToArray();

                    int modIndex = map.Count;
                    var mod = MapBranch(System.Array.IndexOf(possibleModules, possibilities[0]), depth + 1, InvertPassage(r));

                    passage.Prepare(moduleIndex, modIndex, map[modIndex].GetComponent<ModuleObject>().lastPassage);
                }
                else
                {
                    GameObject[] possibilities = possibleModules.Where(
                        m => m.GetComponent<ModuleObject>().passages[InvertPassage(r)].Count > 0
                        ).ToArray();

                    int modIndex = map.Count;
                    int index = Random.Range(0, possibilities.Length - 1);
                    var mod = MapBranch(System.Array.IndexOf(possibleModules, possibilities[index]), depth + 1, InvertPassage(r));

                    //var asd = map[0].GetComponent<ModuleObject>().passages[0];
                    //map[0].GetComponent<ModuleObject>().passages[0].RemoveAt(0);
                    //var sda = map[1].GetComponent<ModuleObject>().passages[0];
                    //Debug.Log(ReferenceEquals(map[1].GetComponent<ModuleObject>().passages[0], map[0].GetComponent<ModuleObject>().passages[0]));

                    var passe = mod.GetComponent<ModuleObject>().passages[InvertPassage(r)].Where(p => !p.connected).ToArray()[0];
                    int passID = mod.GetComponent<ModuleObject>().passages[InvertPassage(r)].IndexOf(passe);

                    passage.Prepare(moduleIndex, modIndex, map[modIndex].GetComponent<ModuleObject>().passages[InvertPassage(r)][passID]);
                }
            }
        }

        return module;
    }
}
