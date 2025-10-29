using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class TransformStateSaver : MonoBehaviour
{
    public Transform root;

    public List<Transform> nodes = new ();
    public Vector3[] localPositions;
    public Quaternion[] localRotations;

    [Button]
    public void Capture()
    {
        nodes.Clear();
        Collect(root);
        localPositions = new Vector3[nodes.Count];
        localRotations = new Quaternion[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            localPositions[i] = nodes[i].localPosition;
            localRotations[i] = nodes[i].localRotation;
        }
    }

    [Button]
    public void Restore()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].localPosition = localPositions[i];
            nodes[i].localRotation = localRotations[i];
        }
    }

    private void Collect(Transform t)
    {
        nodes.Add(t);
        for (int i = 0; i < t.childCount; i++)
        {
            Collect(t.GetChild(i));
        }
    }
}
