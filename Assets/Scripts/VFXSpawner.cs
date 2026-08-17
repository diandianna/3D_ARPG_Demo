using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
        public GameObject prefab;

        public void Spawn()
        {
            Instantiate(prefab, transform.position, transform.rotation);
        }
    
}
