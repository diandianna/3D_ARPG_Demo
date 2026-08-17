using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Range_Detaction : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (transform.parent.gameObject.GetComponent<Auto>() != null)
            {
                Debug.Log("发现玩家");
                transform.parent.gameObject.GetComponent<Auto>().targetPos = other.gameObject.transform;
            }
        }
    }
}
