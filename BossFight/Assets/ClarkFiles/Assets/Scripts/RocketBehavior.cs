using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RocketBehavior : MonoBehaviour
{
    [SerializeField] Rigidbody rocketRB;
    [SerializeField] float rocketSpeed;
    [SerializeField] float rocketDespawnTime;
    [SerializeField] AudioClip rocketFire;

    private void Start()
    {
        rocketRB.AddForce(transform.forward * rocketSpeed * Time.deltaTime, ForceMode.Impulse);
        StartCoroutine(DestroyRocket());
        SoundEffectsManager.instance.PlayAudioClip(rocketFire, true);
    }

    IEnumerator DestroyRocket()
    {
        yield return new WaitForSeconds(rocketDespawnTime);
        Destroy(this.gameObject);
    }
}
