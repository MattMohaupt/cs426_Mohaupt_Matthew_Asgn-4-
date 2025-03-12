using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// adding namespaces
using Unity.Netcode;
using UnityEditor;
// because we are using the NetworkBehaviour class
// NewtorkBehaviour class is a part of the Unity.Netcode namespace
// extension of MonoBehaviour that has functions related to multiplayer
public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    
    public float rotationSpeed = 90;
    // create a list of colors
    public List<Color> colors = new List<Color>();

    // getting the reference to the prefab
    [SerializeField]
    private GameObject spawnedPrefab;
    // save the instantiated prefab
    private GameObject instantiatedPrefab;

    public GameObject cannon;
    public GameObject bullet;

    // reference to the camera audio listener
    [SerializeField] private AudioListener audioListener;
    // reference to the camera
    [SerializeField] private Camera playerCamera;

    Transform t;



    // Start is called before the first frame update
    void Start()
    {
        t = GetComponent<Transform>();
    }
    // Update is called once per frame
    void Update()
    {
        // check if the player is the owner of the object
        // makes sure the script is only executed on the owners 
        // not on the other prefabs 
        if (!IsOwner) return;

        Vector3 moveDirection = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += t.forward * speed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection -= t.forward * speed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            t.rotation *= Quaternion.Euler(0, - rotationSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            t.rotation *= Quaternion.Euler(0, rotationSpeed * Time.deltaTime, 0);
        }
        transform.position += moveDirection * speed * Time.deltaTime;


        // if I is pressed spawn the object 
        // if J is pressed destroy the object
        // if e then push
        if (Input.GetKeyDown(KeyCode.I))
        {
            //instantiate the object
            instantiatedPrefab = Instantiate(spawnedPrefab);
            // spawn it on the scene
            instantiatedPrefab.GetComponent<NetworkObject>().Spawn(true);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            //despawn the object
            instantiatedPrefab.GetComponent<NetworkObject>().Despawn(true);
            // destroy the object
            Destroy(instantiatedPrefab);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
           TryPushRock();
        }
        
        if(IsServer && IsOwner)
        {
        if (Input.GetButtonDown("Fire1"))
        {
            // call the BulletSpawningServerRpc method
            // as client can not spawn objects
            BulletSpawningServerRpc(cannon.transform.position, cannon.transform.rotation);
        }
        }

        

    }

    // this method is called when the object is spawned
    // we will change the color of the objects
    public override void OnNetworkSpawn()
    {
        GetComponent<MeshRenderer>().material.color = colors[(int)OwnerClientId];

        // check if the player is the owner of the object
        if (!IsOwner) return;
        // if the player is the owner of the object
        // enable the camera and the audio listener
        audioListener.enabled = true;
        playerCamera.enabled = true;
    }

    // need to add the [ServerRPC] attribute
    [ServerRpc]
    // method name must end with ServerRPC
    private void BulletSpawningServerRpc(Vector3 position, Quaternion rotation)
    {
        // call the BulletSpawningClientRpc method to locally create the bullet on all clients
        BulletSpawningClientRpc(position, rotation);
    }

    [ClientRpc]
    private void BulletSpawningClientRpc(Vector3 position, Quaternion rotation)
    {
        
        Vector3 spawnOffset = t.forward * 2f; // Move bullet forward to prevent self-collision
        GameObject newBullet = Instantiate(bullet, position + spawnOffset, rotation);
        newBullet.GetComponent<NetworkObject>().Spawn(true);
        newBullet.GetComponent<Bullet>().SetShooterIdServerRpc(OwnerClientId);
        if (newBullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        { 
            rb.linearVelocity = newBullet.transform.forward * 20f; // Adjust speed as needed
        }
        else
        {
        Debug.LogError("Bullet does not have a Rigidbody!");
        }   
        // newBullet.GetComponent<Rigidbody>().linearVelocity += Vector3.up * 2;
        // newBullet.GetComponent<Rigidbody>().AddForce(newBullet.transform.forward * 1500);
        // newBullet.GetComponent<NetworkObject>().Spawn(true);
    }

    void TryPushRock(){
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1.5f)) // Adjust distance if needed
        {
            if (hit.transform.name.Contains("Rock"))
            {
                Rigidbody rock = hit.rigidbody;
                if (rock != null)
                {
                    Vector3 pushDirection = transform.forward;
                    rock.linearVelocity = pushDirection * speed;
                }
            }
        }
    }
}