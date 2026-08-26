using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Range_Detaction : MonoBehaviour
{
    float distance = 100f;
    float newDistance = 100f;

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
                newDistance = Vector3.Distance(other.gameObject.transform.position, gameObject.transform.position);
                if (newDistance < distance)
                {
                    transform.parent.gameObject.GetComponent<Auto>().targetPos = other.gameObject.transform;
                    distance = newDistance;
                }
            }
            else if (transform.parent.gameObject.GetComponent<BossAuto>() != null)
            {
                Debug.Log("发现玩家");
                newDistance = Vector3.Distance(other.gameObject.transform.position, gameObject.transform.position);
                if (newDistance < distance)
                {
                    transform.parent.gameObject.GetComponent<BossAuto>().targetPos = other.gameObject.transform;
                    distance = newDistance;
                }
            }
        }
    }
}
