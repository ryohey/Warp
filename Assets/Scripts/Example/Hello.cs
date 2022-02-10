using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hello : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var pos = transform.localPosition;
        pos.x += 0.1f;
        transform.localPosition = pos;
    }
}
