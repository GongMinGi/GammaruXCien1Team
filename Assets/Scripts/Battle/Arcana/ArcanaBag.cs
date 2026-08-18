using System.Collections.Generic;

public sealed class ArcanaBag
{
    private readonly ArcanaData[] pool;
    private readonly Queue<ArcanaData> currentBag = new();
    private readonly System.Random random;

    public ArcanaBag(IReadOnlyList<ArcanaData> selectedPool, int? seed = null)
    {
        pool = new ArcanaData[selectedPool.Count];
        for (int i = 0; i < selectedPool.Count; i++)
            pool[i] = selectedPool[i];

        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public ArcanaData Draw()
    {
        if (currentBag.Count == 0)
            Refill();

        return currentBag.Dequeue();
    }

    private void Refill()
    {
        ArcanaData[] shuffled = new ArcanaData[pool.Length];
        System.Array.Copy(pool, shuffled, pool.Length);

        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (ArcanaData card in shuffled)
            currentBag.Enqueue(card);
    }
}
