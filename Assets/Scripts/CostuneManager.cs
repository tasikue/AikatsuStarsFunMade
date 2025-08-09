using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Costume
{
    public string id;
    public string name;
    public string brand;
    public string type;
    public string imagePath;
}

[System.Serializable]
public class CostumeList
{
    public List<Costume> costumes;
}

public class CostumeManager : MonoBehaviour
{
    public List<Costume> costumeList;
    public static Costume selectedCostume; // 選択結果を保持

    void Start()
    {
        LoadCostumes();
    }

    void LoadCostumes()
    {
        TextAsset json = Resources.Load<TextAsset>("costumes");
        if (json != null)
        {
            CostumeList list = JsonUtility.FromJson<CostumeList>(json.text);
            costumeList = list.costumes;
        }
        else
        {
            Debug.LogError("costumes.json が見つかりません");
        }
    }

    public void SelectCostume(int index)
    {
        if (index >= 0 && index < costumeList.Count)
        {
            selectedCostume = costumeList[index];
            Debug.Log("選択中のコーデ: " + selectedCostume.name);
        }
    }
}
