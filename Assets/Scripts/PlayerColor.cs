using Fusion;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    public MeshRenderer MeshRenderer;

    [Networked,OnChangedRender(nameof(ColorChanged))]
    public Color NetworkedColor { get; set; }
    void ColorChanged()
    {
        MeshRenderer.material.color = NetworkedColor;
    }
    void Update()
{
    if (HasStateAuthority && Input.GetKeyDown(KeyCode.E))
    {
        // Changing the material color here directly does not work since this code is only executed on the client pressing the button and not on every client.
        NetworkedColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
    }
}
}

