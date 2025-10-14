using Game.Scripts.Player;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager _in;
    public RobotRegistry registry;
    private void Awake() => _in = this;
    public static PlayerRoot GetPrefab(string code) => _in.registry.GetPrefab(code);
    public static Sprite GetIcon(string code) => _in.registry.GetIcon(code);
}