using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ChangeColor : MonoBehaviour
{
    public bool applyColorChange = true;

    private MeshRenderer mesh;

    void Start()
    {
        if (applyColorChange)
        {
            mesh = GetComponent<MeshRenderer>();
            mesh.material.color = Random.ColorHSV();
        }
    }

}
