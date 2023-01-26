using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatReference
{
    public bool useConstant = true;
    public float constantValue;
    public FloatVariable variable;

    public float Value
    {
        get { return useConstant ? constantValue : variable.Value; }
    }
}
