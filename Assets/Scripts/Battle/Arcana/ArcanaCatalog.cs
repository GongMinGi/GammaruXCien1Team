using UnityEngine;

[CreateAssetMenu(fileName = "ArcanaCatalog", menuName = "Battle/Arcana Catalog")]
public class ArcanaCatalog : ScriptableObject
{
    public const int MinArcanaId = 0;
    public const int MaxArcanaId = 21;
    public const int ArcanaCount = 22;

    [SerializeField] private ArcanaData[] entries;

    public ArcanaData GetById(int id)
    {
        if (id < MinArcanaId || id > MaxArcanaId)
            return null;

        if (entries == null)
            return null;

        foreach (ArcanaData entry in entries)
        {
            if (entry != null && entry.Id == id)
                return entry;
        }

        return null;
    }
}
