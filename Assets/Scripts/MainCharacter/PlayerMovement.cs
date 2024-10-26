using UnityEngine;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    [Header("View Settings")]
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    public bool invertLook;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private float activeMoveSpeed;

    [Header("Gravity Settings")]
    private const float gravity = -9.81f;
    private float yVelocity;

    [Header("Running Sway Effect")]
    public float swayAmount = 1.5f; // The amount of weapon tilt or sway
    public float swaySpeed = 4f; // How fast the effect happens
    public Vector3 runWeaponOffset = new Vector3(0f, -0.2f, 0.2f); // Weapon offset when running

    [Header("Weapon System")]
    public Transform[] weapons;  // Array of weapons (or list)
    private Transform activeWeapon;  // Currently active weapon

    [Header("References")]
    public CharacterController characterController;

    private Camera cam;
    private float verticalRotStore;
    private Vector2 mouseInput;
    private Vector3 moveDir, movement;

    public GameObject bulletImpact;
    //public float timeBetweenShots = .1f;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 10f, /* heatPerShot = 1f */coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    public Guns[] allGuns;
    private int selectedItem;

    [Header("Camera Bob Settings")]
    public float bobbingAmount = 5.1f; // The amount the camera bobs up and down
    public float bobbingSpeed = 14f; // Speed of the bobbing effect
    private float timer = 5f; // Timer for bobbing

    void Start()
    {
        SetActiveWeapon(0);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        cam = Camera.main;

        UIController.instance.weaponTempSlider.maxValue = maxHeat;
        switchItem();

        Transform newTransform = SpawnManager.instance.GetSpawnPoint();
        transform.position = newTransform.position;
        transform.rotation = newTransform.rotation;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCameraBobbing();

        if (Input.GetKeyDown(KeyCode.Alpha1)) { SetActiveWeapon(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { SetActiveWeapon(1); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { SetActiveWeapon(2); }
    }

    private void HandleMouseLook()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

        if (invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
    }

    private void HandleMovement()
    {
        activeMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;

        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        Vector3 direction = (transform.forward * moveDir.z) + (transform.right * moveDir.x);
        movement = direction.normalized * activeMoveSpeed;

        if (characterController.isGrounded)
        {
            yVelocity = 0f;
            if (Input.GetButtonDown("Jump"))
            {
                yVelocity = Mathf.Sqrt(2f * -gravity * 1.5f); // Adjust jump height as needed
            }
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        movement.y = yVelocity;
        characterController.Move(movement * Time.deltaTime);

        if (allGuns[selectedItem].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;
            if (muzzleCounter <= 0)
            {
                allGuns[selectedItem].muzzleFlash.SetActive(false);
            }
        }

        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
            if (Input.GetMouseButton(0) && allGuns[selectedItem].isAutomatic)
            {
                shotCounter -= Time.deltaTime;

                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }

            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overheatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                overHeated = false;
                UIController.instance.overheatedMessage.gameObject.SetActive(false);
            }
        }

        if (heatCounter < 0)
        {
            heatCounter = 0f;
        }
        UIController.instance.weaponTempSlider.value = heatCounter;


        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedItem++;

            if (selectedItem >= allGuns.Length)
            {
                selectedItem = 0;
            }
            switchItem();
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedItem--;
            if (selectedItem < 0)
            {
                selectedItem = allGuns.Length - 1;
            }
            switchItem();
        }

        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString())) 
            {
                selectedItem = i;
                switchItem();
            }
        }













        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        else if (UnityEngine.Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        ray.origin = cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("we hit it!" + hit.collider);

            GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
            Destroy(bulletImpactObject, 10f);
        }

        shotCounter = allGuns[selectedItem].timeBetweenShots;

        heatCounter += allGuns[selectedItem].heatPerShot;
        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedItem].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    private void LateUpdate()
    {
        cam.transform.position = viewPoint.position;
        cam.transform.rotation = viewPoint.rotation;
    }

    void switchItem()
    {
        foreach (Guns gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[selectedItem].gameObject.SetActive(true);
    }

    private void HandleCameraBobbing()
    {
        // Check if the player is grounded and running
        if (characterController.isGrounded)
        {
            timer += Time.deltaTime * bobbingSpeed;
            float newY = Mathf.Sin(timer) * bobbingAmount; // Calculate the new Y position based on the sine function

            cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, newY, cam.transform.localPosition.z);
        }
        else
        {
            // Reset the camera's Y position when not running
            cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, 0, cam.transform.localPosition.z);
        }
    }
    private void HandleRunningEffect()
    {
        if (Input.GetKey(KeyCode.LeftShift) && characterController.velocity.magnitude > 0.1f)
        {
            // Apply weapon sway effect to the currently active weapon
            Vector3 swayPosition = activeWeapon.localPosition + runWeaponOffset;
            activeWeapon.localPosition = Vector3.Lerp(activeWeapon.localPosition, swayPosition, Time.deltaTime * swaySpeed);

            // You can also rotate the weapon slightly
            activeWeapon.localRotation = Quaternion.Slerp(activeWeapon.localRotation, Quaternion.Euler(5f, 0f, 0f), Time.deltaTime * swaySpeed);
        }
        else
        {
            // Reset the weapon position and rotation when not running
            activeWeapon.localPosition = Vector3.Lerp(activeWeapon.localPosition, Vector3.zero, Time.deltaTime * swaySpeed);
            activeWeapon.localRotation = Quaternion.Slerp(activeWeapon.localRotation, Quaternion.identity, Time.deltaTime * swaySpeed);
        }
    }

    // Method to switch active weapons
    public void SetActiveWeapon(int weaponIndex)
    {
        // Deactivate all weapons
        foreach (var weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }

        // Activate the selected weapon
        activeWeapon = weapons[weaponIndex];
        activeWeapon.gameObject.SetActive(true);
    }
}