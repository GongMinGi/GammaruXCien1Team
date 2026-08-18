using UnityEngine;

public class GridCell : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    public int X => x;
    public int Y => y;

    public void Initialize(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
