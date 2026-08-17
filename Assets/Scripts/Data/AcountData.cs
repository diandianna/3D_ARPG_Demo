using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcountData
{
    public string acountName;
    public string password;

    public AcountData(string acountName, string password)
    {
        this.acountName = acountName;
        this.password = password;
    }

    AcountData()
    {

    }
}
