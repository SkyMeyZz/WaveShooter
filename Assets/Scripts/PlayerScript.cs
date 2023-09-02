using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerScript : NetworkBehaviour
{
    [Header("Movements")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float dashSpeedMultiplier;
    [SerializeField] private float dashTime;
    [SerializeField] private GameObject dashParticle;
    private Vector2 movement;
    private enum MovementState { normal, dodgeRolling };
    private MovementState currentMovementState;
    private bool canDodgeRoll;
    private Vector2 dodgeDir;
    private float dodgingTimer;

    [Header("Camera")]
    private Camera cam;
    [SerializeField] private GameObject cameraPoint;
    private Vector3 mousePos;

    [Header("Weapon Handling")]
    [SerializeField]
    private List<GameObject> weapons;
    [SerializeField] private GameObject weaponSpot;
    [SerializeField] private GameObject currentWeapon;
    //private WeaponScript weaponScript;
    private int weaponListIndex;

    private Rigidbody2D rb;
    private Rigidbody2D weaponRb;

    public PlayerScript localInstance { get; private set; }

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        currentMovementState = MovementState.normal;
        canDodgeRoll = true;

    }
    public override void OnNetworkSpawn()
    {
        //cam = CameraManager.instance.mainCamera;

        if (!IsOwner) return;
        //CameraManager.instance.virtualCamera.Follow = cameraPoint.transform;
        localInstance = this;
    }

    private void Update()
    {
        //HandleWeapon();
        //SelectWeapon();

        if (!IsOwner) return;
        HandleInputs();


        //cameraPoint.transform.position = Vector3.Lerp(rb.position, mousePos, 0.40f); //calcule et assigne la position de l'objet cameraPoint utilisé pour la position de la camera
    }

    private void FixedUpdate()
    {
        if (currentMovementState == MovementState.normal)
        {
            rb.position += movement.normalized * moveSpeed * Time.fixedDeltaTime;
        }
        else if (currentMovementState == MovementState.dodgeRolling)
        {
            DodgeRoll();
        }
    }

    /*private void HandleWeapon()
    {
        if (weapons.Count != 0)
        {
            weaponSpot.SetActive(true);
            weaponScript = currentWeapon.GetComponent<WeaponScript>();


            if (weaponScript == null) { Debug.LogError("Couldn't find the WeaponScript script inside the current helded weapon : " + currentWeapon.name); }

            //Handle WeaponSpot rotation
            Vector2 lookDir = mousePos - weaponScript.GetWeaponShootPoint().transform.position;
            lookDir = lookDir.normalized;
            float minOrientationRange = 2f;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            if (Vector2.Distance(transform.position, mousePos) > minOrientationRange)
            {
                weaponScript.transform.eulerAngles = new Vector3(0, 0, angle);
            }
            Debug.DrawRay(weaponScript.GetWeaponShootPoint().transform.position, weaponScript.GetWeaponShootPoint().transform.right * 10);

            //Handle gun flip and arm rotation
            if (weaponScript.GetWeaponSpriteHolder().GetComponent<SpriteRenderer>().flipY)
            {
                weaponSpot.transform.localPosition = new Vector2(-0.65f, -0.25f);
                if (weaponScript.transform.localEulerAngles.z < 45 || weaponScript.transform.localEulerAngles.z >= 325)
                {
                    weaponScript.GetWeaponSpriteHolder().GetComponent<SpriteRenderer>().flipY = false;
                }
            }
            else if (!weaponScript.GetWeaponSpriteHolder().GetComponent<SpriteRenderer>().flipY)
            {
                weaponSpot.transform.localPosition = new Vector2(0.65f, -0.25f);
                if (weaponScript.transform.localEulerAngles.z >= 135 && weaponScript.transform.localEulerAngles.z < 225)
                {
                    weaponScript.GetWeaponSpriteHolder().GetComponent<SpriteRenderer>().flipY = true;
                }
            }
        }
        else
        {
            weaponSpot.SetActive(false);
        }
    }*/

    private void HandleInputs()
    {
        if (currentMovementState != MovementState.dodgeRolling)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        //mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            //ScollUp();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            //ScollDown();
        }

        /*if (Input.GetKeyDown(KeyCode.E))
        {
            IInteractable interactable = GetPlayerClosestInterraction();
            if (interactable == null) return;

            interactable.Interact(gameObject);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            DropWeapon();
        }*/

        if (Input.GetKeyDown(KeyCode.Space) && canDodgeRoll)
        {
            HandleDodgeRolling();
        }
    }

    /*private IInteractable GetPlayerClosestInterraction()
    {
        List<IInteractable> interactablesList = new List<IInteractable>();
        float interactRange = 2f;
        Collider2D[] colliderArray = Physics2D.OverlapCircleAll(transform.position, interactRange);
        foreach (Collider2D collider in colliderArray)
        {
            if (collider.TryGetComponent(out IInteractable interactables))
            {
                interactablesList.Add(interactables);
            }
        }

        IInteractable closestInteractable = null;
        foreach (IInteractable interactables in interactablesList)
        {
            if (closestInteractable == null)
            {
                closestInteractable = interactables;
            }
            else
            {
                if (Vector3.Distance(transform.position, interactables.GetTransform().position) < Vector3.Distance(transform.position, closestInteractable.GetTransform().position))
                {
                    closestInteractable = interactables;
                }
            }
        }

        return closestInteractable;
    }

    private void ScollDown()
    {
        if (weaponListIndex <= 0)
            weaponListIndex = weapons.Count - 1;
        else
            weaponListIndex--;
    }

    private void ScollUp()
    {
        if (weaponListIndex >= weapons.Count - 1)
            weaponListIndex = 0;
        else
            weaponListIndex++;
    }

    public void AddWeapon(GameObject weapon)
    {
        weapons.Add(weapon);
        weaponListIndex = weapons.Count - 1;
        currentWeapon = weapons[weapons.Count - 1];
    }

    public void DropWeapon()
    {
        currentWeapon.GetComponent<WeaponScript>().Drop();
        ScollDown();
        weapons.Remove(currentWeapon);
    }

    private void SelectWeapon()
    {
        int i = 0;
        foreach (GameObject weapon in weapons)
        {
            if (i == weaponListIndex)
            {
                weapon.SetActive(true);
                currentWeapon = weapons[i];
            }
            else
                weapon.SetActive(false);
            i++;
        }
    }*/

    private void HandleDodgeRolling()
    {
        //Instantiate(dashParticle, transform.position, transform.rotation);
        dodgeDir = movement;
        dodgingTimer = dashTime;
        currentMovementState = MovementState.dodgeRolling;
    }

    private void DodgeRoll()
    {
        rb.position += dodgeDir.normalized * moveSpeed * dashSpeedMultiplier * Time.deltaTime;
        dodgingTimer -= Time.deltaTime;
        if (dodgingTimer <= 0)
        {
            currentMovementState = MovementState.normal;
        }
    }

    /*public GameObject GetWeaponSpot()
    {
        return weaponSpot;
    }

    public void SetCurrentWeapon(GameObject newCurrentWeapon)
    {
        currentWeapon = newCurrentWeapon;
    }

    public Vector3 GetMousePos()
    {
        return mousePos;
    }*/
}

