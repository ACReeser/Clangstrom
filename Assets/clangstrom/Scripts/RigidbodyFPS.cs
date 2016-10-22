using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public enum WeaponState { Sniper_Rifle = 0, Laser_Beam, Grenade_Launcher }
[Flags]
public enum CamouflageState { None = 0, Visual, Thermal, Echo }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]

public class RigidbodyFPS : NetworkBehaviour
{
    public Camera mainCam, thermalCam, echoCam;
    public float speed = 10.0f;
    public float gravity = 10.0f;
    public float maxVelocityChange = 10.0f;
    public bool canJump = true;
    public float jumpHeight = 2.0f;
    public GameObject rocketPrefab, smokePrefab;
    public ViewState InitialSpectrum { get; set; }
    public bool UseGravity = true;
    public VisualCamo activeVisualCamo;
    public Light flashlight;
    public MeshRenderer[] pingVizs;
    public Animator WeaponAnimator;

    private bool grounded = false;
    private Rigidbody rigid;
    private Collider collide;
    private Player player;
    private RocketLauncher rocket;
    private SniperRifle sniper;
    private LaserBeam laser;

    private ViewState _currentSpectrum;
    private AudioListener listener;
    private SmoothMouseLook look;
    private Pinger pinger;

    private float _defaultFOV = 60f;
    private float focusedFOV = 30f;
    private float focusSpeed = 3f;
    private float focusValue = 1f;

    [SyncVar]
    public bool IsPinging = false;
    private GUIManager gui;

    [SyncVar(hook = "OnFlashlightChange")]
    public bool FlashlightOn = false;

    [SyncVar(hook = "OnWeaponChange")]
    public WeaponState ActiveWeapon;

    [SyncVar(hook = "OnDistortion")]
    public float DistortionAmount;

    [SyncVar(hook = "OnCamoChange")]
    public CamouflageState ActiveCamouflage = CamouflageState.None;

    public WeaponState[] Loadout = new WeaponState[] { WeaponState.Sniper_Rifle, WeaponState.Laser_Beam, WeaponState.Grenade_Launcher };
    private int ActiveLoadoutSlot = 0;

    private ViewState CurrentSpectrum
    {
        get
        {
            return _currentSpectrum;
        }
        set
        {
            SetSpectrum(value);
            _currentSpectrum = value;
        }
    }

    public bool Paused { get; set; }

    public void OnWeaponChange(WeaponState newWeapon)
    {
        ActiveWeapon = newWeapon;

        if (isLocalPlayer)
        {
            gui.UpdateWeapon(this.ActiveWeapon);
        }

        WeaponAnimator.SetInteger("WeaponState", (int)newWeapon);
    }

    public void OnCamoChange(CamouflageState newFlags)
    {
        ActiveCamouflage = newFlags;

        this.activeVisualCamo.SetState(HasCamoOn(CamouflageState.Visual));
    }

    private void SetSpectrum(ViewState value)
    {
        mainCam.enabled = (value == ViewState.Visual);
        echoCam.enabled = (value == ViewState.Echolocation);
        thermalCam.enabled = (value == ViewState.Thermal);

        if (this.IsPinging != (value == ViewState.Echolocation))
        {
            CmdSetPing(value == ViewState.Echolocation);
        }

        gui.UpdateCurrentSpectrum(value);
    }

    [Command]
    private void CmdSetFlashlight(bool isOn)
    {
        this.FlashlightOn = isOn;
    }

    [Command]
    private void CmdSetPing(bool isPinging)
    {
        this.IsPinging = isPinging;
    }

    [Command]
    private void CmdSetWeapon(WeaponState newWeapon)
    {
        this.ActiveWeapon = newWeapon;
        this.laser.Sighting = newWeapon == WeaponState.Laser_Beam;
    }

    [Command]
    public void CmdSetCamouflage(CamouflageState spectrum, bool isOn)
    {
        if (isOn)
            ActiveCamouflage |= spectrum; //on
        else
            ActiveCamouflage &= ~spectrum; //off
    }

    void Awake()
    {
        this.rigid = GetComponent<Rigidbody>();
        this.collide = GetComponent<Collider>();
        this.listener = mainCam.GetComponent<AudioListener>();
        this.look = GetComponent<SmoothMouseLook>();
        rigid.freezeRotation = true;
        rigid.useGravity = false;

        this.player = gameObject.AddComponent<Player>(); ;
        this.sniper = GetComponent<SniperRifle>();
        this.laser = GetComponent<LaserBeam>();
        this.pinger = echoCam.GetComponent<Pinger>();
        this.gui = GetComponent<GUIManager>();
        listener.enabled = mainCam.enabled = echoCam.enabled = thermalCam.enabled = this.look.enabled = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        rocket = gameObject.AddComponent<RocketLauncher>();
        rocket.bulletPrefab = rocketPrefab;
        rocket.collide = collide;
        sniper.smokeTrail = smokePrefab;

        GUIAnchor.Instance.HUDCanvas.enabled = true;
        gui.UpdateWeapon(this.ActiveWeapon);
    }

    public override void OnStartLocalPlayer()
    {
        listener.enabled = look.enabled = true;
        CurrentSpectrum = InitialSpectrum;
        foreach(MeshRenderer pingMesh in pingVizs)
        {
            pingMesh.enabled = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        bool shiftDown = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (shiftDown)
                CmdSetCamouflage(CamouflageState.Visual, !HasCamoOn(CamouflageState.Visual));
            else
                CurrentSpectrum = ViewState.Visual;
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            CurrentSpectrum = ViewState.Thermal;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            CurrentSpectrum = ViewState.Echolocation;
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            CurrentSpectrum = CurrentSpectrum.Next();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            CmdSetFlashlight(!FlashlightOn);
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Command function is called from the client, but invoked on the server
            //this.rocket.CmdFire();
            //UnityEditor.EditorApplication.isPaused = true;
            switch (ActiveWeapon)
            {
                case WeaponState.Sniper_Rifle:
                    this.sniper.CmdFire(this.transform.position, this.transform.forward);
                    break;
                case WeaponState.Laser_Beam:
                    this.laser.CmdFire(true);
                    break;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            switch (ActiveWeapon)
            {
                case WeaponState.Laser_Beam:
                    this.laser.CmdFire(false);
                    break;
            }
        }

        bool rightClick = Input.GetMouseButton(1),
             needZoomIn = rightClick && mainCam.fieldOfView != focusedFOV,
             needZoomOut = !rightClick && mainCam.fieldOfView != _defaultFOV;

        if (needZoomIn)
        {
            focusValue -= focusSpeed * Time.deltaTime;

            float newFOV = Mathfx.Hermite(focusedFOV, _defaultFOV, Mathf.Clamp(focusValue, 0f, 1f));
            
            mainCam.fieldOfView = thermalCam.fieldOfView = echoCam.fieldOfView = newFOV;
        }
        else if (needZoomOut)
        {
            focusValue += focusSpeed * Time.deltaTime;

            float newFOV = Mathfx.Hermite(focusedFOV, _defaultFOV, Mathf.Clamp(focusValue, 0f, 1f));

            mainCam.fieldOfView = thermalCam.fieldOfView = echoCam.fieldOfView = newFOV;
        }

        if (Input.mouseScrollDelta.y > 0f)
        {
            ChangeWeapon(1);
        }
        else if (Input.mouseScrollDelta.y < 0f)
        {
            ChangeWeapon(-1);
        }
    }

    public bool HasCamoOn(CamouflageState state)
    {
        return (ActiveCamouflage & state) == state;
    }

    private void ChangeWeapon(int delta)
    {
        int newLoadIndex = ActiveLoadoutSlot + delta;
        if (newLoadIndex > Loadout.Length - 1)
            newLoadIndex = 0;
        else if (newLoadIndex < 0)
            newLoadIndex = Loadout.Length - 1;

        ActiveLoadoutSlot = newLoadIndex;
        
        CmdSetWeapon(Loadout[ActiveLoadoutSlot]);
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (grounded)
        {
            // Calculate how fast we should be moving
            Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            DistortionAmount = Math.Max(Math.Abs(targetVelocity.x), Math.Abs(targetVelocity.z));

            targetVelocity = transform.TransformDirection(targetVelocity);
            targetVelocity *= speed;

            // Apply a force that attempts to reach our target velocity
            Vector3 velocity = rigid.velocity;
            Vector3 velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
            velocityChange.y = 0;
            rigid.AddForce(velocityChange, ForceMode.VelocityChange);

            // Jump
            if (canJump && Input.GetButton("Jump"))
            {
                rigid.velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
            }
        }

        if (UseGravity)
        {
            // We apply gravity manually for more tuning control
            rigid.AddForce(new Vector3(0, -gravity * rigid.mass, 0));
        }

        grounded = false;
    }

    void OnCollisionStay()
    {
        grounded = true;
    }

    float CalculateJumpVerticalSpeed()
    {
        // From the jump height and gravity we deduce the upwards speed 
        // for the character to reach at the apex.
        return Mathf.Sqrt(2 * jumpHeight * gravity);
    }

    public void OnDistortion(float distortionAmount)
    {
        DistortionAmount = distortionAmount;
        this.activeVisualCamo.MovementAmount = distortionAmount;
    }

    public void OnFlashlightChange(bool flashlightOn)
    {
        FlashlightOn = flashlightOn;
        flashlight.enabled = flashlightOn;
    }
}
