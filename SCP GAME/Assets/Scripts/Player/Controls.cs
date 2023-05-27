using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controls : MonoBehaviour
{

    public static Controls i;

    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode crouchKey = KeyCode.C;
    [SerializeField] KeyCode shootKey = KeyCode.Mouse0;

    public void Awake()
    {
        i = this;
    }
    public KeyCode SprintKey
    {
        get { return sprintKey; }
    }
    public KeyCode JumpKey
    {
        get { return jumpKey; }
    }
    public KeyCode CrouchKey
    {
        get { return crouchKey; }
    }

    public KeyCode ShootKey
    {
        get { return shootKey; }
    }
}
