using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {

    public enum Character {
        Sonic,
        Tails,
        Knuckles
    }

    public enum State {
        Stand,
        Lookup,
        Crouch,
        Walk,
        Spin,
        Spindash,
        Jump,
        Airwalk,
        Airspin,
        Bounce,
        Peelout,
        Fly,
        Flyhang,
        Glide,
        Glidefall,
        Glideslide,
        Glideland,
        Wallcling,
        Wallmount,
        Hurt,
        Die
    }

    public enum Shield {
        None,
        Blue,
        Bubble,
        Fire,
        Lightning
    }

    [Header("Options")]
    public bool sonicDropDash = true;
    public bool sonicPeelOut = false;
    public bool sonicInstaShield = false;
    public bool knucklesShortJump = true;

    [Header("Enumerated Variables")]
    public Character currentCharacter;
    public State currentState;
    public Shield currentShield;

    [Header("Scripted Managers")]
    public PlayerInputManager InputManager;
    public SpriteManager PlayerSpriteManager;
    public SpriteManager TwoTailsSpriteManager;
    public SpriteManager ShieldSpriteManager;
    public SpriteManager Shield2SpriteManager;
    public PlayerSFXManager SFXManager;

    [Header("Game Objects")]
    public GameObject cameraFocus;

    [Header("Prefabs")]
    public GameObject particleSkiddustPrefab;
    public GameObject particleSpindashdustPrefab;
    public GameObject particleDropdashdustPrefab;
    public GameObject particleInstashieldPrefab;
    public GameObject particleLightningjumpsparksPrefab;

    [Header("Parameters")]
    public float xScale;
    public float yScale;

    float bodyWidthRadius;
    float bodyHeightRadius;
    float bodyPushRadius;

    RaycastHit SensorAHit;
    RaycastHit SensorBHit;
    RaycastHit SensorCHit;
    RaycastHit SensorDHit;
    RaycastHit SensorEHit;
    RaycastHit SensorFHit;

    Collider SensorACollider;
    Collider SensorBCollider;
    Collider SensorCCollider;
    Collider SensorDCollider;
    Collider SensorECollider;
    Collider SensorFCollider;

    float xsp;
    float ysp;
    float gsp;

    float ang;

    float spinrev;

    float flygrv;

    float gldspd;
    float gldang;

    float acc = 0.046875f;
    float dec = 0.5f;
    float frc = 0.046875f;
    float top = 6.0f;
    float slp = 0.125f;

    float rollfrc = 0.0234375f;
    float rolldec = 0.25f;
    float slprollup = 0.089125f;
    float slprolldown = 0.3125f;
    float rolltop = 16.0f;
    float fall = 2.5f;

    float air = 0.09375f;
    float jmp = 6.5f;
    float grv = 0.21875f;

    float drpspd = 8.0f;
    float drpmax = 12.0f;

    float gldgrv = 0.125f;
    float gldmax = 24.0f;

    bool isFacingRight;
    bool isGrounded;

    bool canPerformShortJump;
    bool performedInstaShield;

    int horizontalControlLockTimer = 0;
    int dropdashTimer;
    int peeloutTimer;
    int flyTimer;
    int glidelandTimer;

    int idleAnimationTimer;
    int skiddustAnimationTimer;
    int glidedustAnimationTimer;

    int spindashSFXCounter;

    Vector3 planeUpVector = Vector3.up;
    Vector3 planeRightVector = Vector3.right;
    Vector3 planePosition = Vector3.zero;
    float planeAlignmentSpeed = 0.0f;

    void Awake() {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        PlayerSpriteManager.SetSpriteSet("sonic");
        TwoTailsSpriteManager.SetSpriteSet("twotails");
        ShieldSpriteManager.SetSpriteSet("shield");
        Shield2SpriteManager.SetSpriteSet("shield");
        ShieldSpriteManager.FlipAnimation(false);
        Shield2SpriteManager.FlipAnimation(false);

        PlayerSpriteManager.PlayAnimation("stand");
        TwoTailsSpriteManager.PlayAnimation("stand", 1.25f);
        ShieldSpriteManager.PlayAnimation("none");
        Shield2SpriteManager.PlayAnimation("none");

        SetCharacter(currentCharacter);
        SetState(currentState);
        SetShield(currentShield);

        isFacingRight = true;
        isGrounded = true;

        xsp = 0.0f;
        ysp = 0.0f;
        gsp = 0.0f;
        ang = 0.0f;

        SetPlane(Vector3.up, Vector3.right, Vector3.zero);
    }

    void Update() {
        UpdateState(currentState);
        if(planeAlignmentSpeed >= 0.0f) {
            UpdatePlane();
        }
    }

    public void UpdateState(State state) {
        switch(state) {
            case State.Stand:
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    CheckCeilingSensors(pushAway: false, updateValues: false);
                    if(SensorECollider == null || SensorFCollider == null) {
                        xsp -= jmp * (float)Math.Sin(ang);
                        ysp -= jmp * (float)Math.Cos(ang);
                        PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        SFXManager.PlayOneShotSFX(SFXManager.jumpSFX);
                        SetState(State.Jump);
                        TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                        TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                    }
                }
                else if(InputManager.GetInputDown(PlayerInputManager.Input.Spin)) {
                    SetState(State.Spindash);
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Up, PlayerInputManager.Input.Down)) {
                    SetState(State.Lookup);
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Down, PlayerInputManager.Input.Up)) {
                    SetState(State.Crouch);
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    gsp -= dec;
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    SetState(State.Walk);
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    gsp += dec;
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    SetState(State.Walk);
                }
                else {
                    if((SensorACollider == null || SensorBCollider == null) &&
                       PlayerSpriteManager.GetAnimationName() != "endlookup" && PlayerSpriteManager.GetAnimationName() != "endcrouch" && 
                       !(Physics.Raycast(transform.position + ConvertVector2ToVector3(Vector2.zero),
                                         -planeUpVector,
                                         maxDistance: bodyHeightRadius + 0.16f,
                                         layerMask: 1 << 8))) {
                        idleAnimationTimer = 0;
                        if(isFacingRight) {
                            if(SensorBCollider == null && PlayerSpriteManager.GetAnimationName() != "frontbalance") {
                                PlayerSpriteManager.PlayAnimation("frontbalance", speed: 0.953125f);
                            }
                            else if(SensorACollider == null && PlayerSpriteManager.GetAnimationName() != "backbalance") {
                                PlayerSpriteManager.PlayAnimation("backbalance", speed: 0.953125f);
                            }
                        }
                        else {
                            if(SensorACollider == null && PlayerSpriteManager.GetAnimationName() != "frontbalance") {
                                PlayerSpriteManager.PlayAnimation("frontbalance", speed: 0.953125f);
                            }
                            else if(SensorBCollider == null && PlayerSpriteManager.GetAnimationName() != "backbalance") {
                                PlayerSpriteManager.PlayAnimation("backbalance", speed: 0.953125f);
                            }
                        }
                        TwoTailsSpriteManager.PlayAnimation("none");
                    }
                    else {
                        if(PlayerSpriteManager.GetAnimationName() == "stand") {
                            idleAnimationTimer++;
                            if(idleAnimationTimer >= 200) {
                                idleAnimationTimer = 0;
                                PlayerSpriteManager.PlayAnimation("idle", "stand", speed: 2.5f);
                            }
                        }
                    }
                }
                CheckWallSensors();
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        SetState(State.Walk);
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Lookup:
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    if(GetCharacter() == Character.Sonic && sonicPeelOut) {
                        SetState(State.Peelout);
                    }
                    else {
                        CheckCeilingSensors(pushAway: false, updateValues: false);
                        if(SensorECollider == null || SensorFCollider == null) {
                            xsp -= jmp * (float)Math.Sin(ang);
                            ysp -= jmp * (float)Math.Cos(ang);
                            PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                            TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                            TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                            SFXManager.PlayOneShotSFX(SFXManager.jumpSFX);
                            SetState(State.Jump);
                        }
                    }
                }
                else if(InputManager.GetInputDown(PlayerInputManager.Input.Spin)) {
                    SetState(State.Spindash);
                }
                else if(!InputManager.GetInput(PlayerInputManager.Input.Up, PlayerInputManager.Input.Down)) {
                    PlayerSpriteManager.PlayAnimation("endlookup", "stand", speed: 2.0f);
                    SetState(State.Stand);
                }
                CheckWallSensors();
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Crouch:
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump) || InputManager.GetInputDown(PlayerInputManager.Input.Spin)) {
                    SetState(State.Spindash);
                }
                else if(!InputManager.GetInput(PlayerInputManager.Input.Down, PlayerInputManager.Input.Up)) {
                    PlayerSpriteManager.PlayAnimation("endcrouch", "stand", speed: 2.0f);
                    SetState(State.Stand);
                }
                CheckWallSensors();
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Walk:
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    CheckCeilingSensors(pushAway: false, updateValues: false);
                    if(SensorECollider == null || SensorFCollider == null) {
                        xsp -= jmp * (float)Math.Sin(ang);
                        ysp -= jmp * (float)Math.Cos(ang);
                        PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                        TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                        SFXManager.PlayOneShotSFX(SFXManager.jumpSFX);
                        SetState(State.Jump);
                    }
                }
                else if(InputManager.GetInputDown(PlayerInputManager.Input.Spin) && 1.0f <= Math.Abs(gsp)) {
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight ? true : false);
                    SFXManager.PlayOneShotSFX(SFXManager.spinSFX);
                    SetState(State.Spin);
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right) && horizontalControlLockTimer <= 0) {
                    gsp -= slp * (float)Math.Sin(ang);
                    if(gsp > 0.0f) {
                        gsp -= dec;
                        if(3.5f <= Math.Abs(gsp) && (5.49778714f <= ang || ang <= 0.78539816f)) {
                            if(PlayerSpriteManager.GetAnimationName() != "startskid" && PlayerSpriteManager.GetAnimationName() != "skid") {
                                skiddustAnimationTimer = 3;
                                PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                                PlayerSpriteManager.PlayAnimation("startskid", "skid", speed: 2.5f, nextSpeed: 2.5f);
                                PlayerSpriteManager.SetAnimationAngleSmooth(0.0f, smoothSpeed: 7.5f);
                                TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                                TwoTailsSpriteManager.PlayAnimation("skid", 1.25f);
                                TwoTailsSpriteManager.SetAnimationAngleSmooth(0.0f, smoothSpeed: 7.5f);
                                SFXManager.PlayOneShotSFX(SFXManager.skidSFX);
                            }
                        }
                        if(gsp <= 0.0f) {
                            gsp = -0.5f;
                            PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                            TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                            if(PlayerSpriteManager.GetAnimationName() == "startskid" || PlayerSpriteManager.GetAnimationName() == "skid") {
                                PlayerSpriteManager.PlayAnimation("skidturn", "walk", nextSpeed: 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                                TwoTailsSpriteManager.PlayAnimation("none");
                            }
                        }
                    }
                    else if(gsp > -top) {
                        gsp -= acc;
                        if(gsp <= -top) {
                            gsp = -top;
                        }
                    }
                    if(CheckWallSensors(pushAway: false, updateValues: false)) {
                        if(SensorECollider != null && horizontalControlLockTimer <= 0) {
                            if(GetState() == State.Walk && PlayerSpriteManager.GetAnimationName() != "push") {
                                PlayerSpriteManager.PlayAnimation("push", 0.75f);
                                TwoTailsSpriteManager.PlayAnimation("spindash", 1.25f);
                            }
                            
                        }
                        else {
                            if(PlayerSpriteManager.GetAnimationName() == "push") {
                                PlayerSpriteManager.PlayAnimation("walk", 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                            }
                        }
                    }
                    else {
                        if(PlayerSpriteManager.GetAnimationName() == "push") {
                            PlayerSpriteManager.PlayAnimation("walk", 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                        }
                    }
                    if(gsp < 0.0f && PlayerSpriteManager.GetAnimationName() != "startskid" && PlayerSpriteManager.GetAnimationName() != "skidturn" && PlayerSpriteManager.GetAnimationName() != "push") {
                        if(10.0f <= Math.Abs(gsp) && PlayerSpriteManager.GetAnimationName() != "dash") {
                            PlayerSpriteManager.PlayAnimationWithOffset("dash");
                        }
                        else if(6.0f <= Math.Abs(gsp) && Math.Abs(gsp) < 10.0f && PlayerSpriteManager.GetAnimationName() != "run") {
                            PlayerSpriteManager.PlayAnimationWithOffset("run");
                        }
                        else if(4.5f <= Math.Abs(gsp) && Math.Abs(gsp) < 6.0f && PlayerSpriteManager.GetAnimationName() != "jog") {
                            PlayerSpriteManager.PlayAnimationWithOffset("jog");
                        }
                        else if(Math.Abs(gsp) < 4.5f && PlayerSpriteManager.GetAnimationName() != "walk") {
                            PlayerSpriteManager.PlayAnimationWithOffset("walk");
                        }
                        PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                        TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                        PlayerSpriteManager.SetAnimationAngleSmooth((5.49778714f <= ang || ang <= 0.78539816f ? 0.0f : ang), smoothSpeed: Math.Max(7.5f, Math.Abs(gsp) * 2.0f));
                        PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                        TwoTailsSpriteManager.PlayAnimation("none");
                    }
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left) && horizontalControlLockTimer <= 0) {
                    gsp -= slp * (float)Math.Sin(ang);
                    if(gsp < 0.0f) {
                        gsp += dec;
                        if(3.5f <= Math.Abs(gsp) && (5.49778714f <= ang || ang <= 0.78539816f)) {
                            if(PlayerSpriteManager.GetAnimationName() != "startskid" && PlayerSpriteManager.GetAnimationName() != "skid") {
                                skiddustAnimationTimer = 3;
                                PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                                PlayerSpriteManager.PlayAnimation("startskid", "skid",  speed: 2.5f, nextSpeed: 2.5f);
                                PlayerSpriteManager.SetAnimationAngleSmooth(0.0f, smoothSpeed: 7.5f);
                                TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                                TwoTailsSpriteManager.PlayAnimation("skid", 1.25f);
                                TwoTailsSpriteManager.SetAnimationAngleSmooth(0.0f, smoothSpeed: 7.5f);
                                SFXManager.PlayOneShotSFX(SFXManager.skidSFX);
                            }
                        }
                        if(gsp >= 0.0f) {
                            gsp = 0.5f;
                            PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                            TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                            if(PlayerSpriteManager.GetAnimationName() == "startskid" || PlayerSpriteManager.GetAnimationName() == "skid") {
                                PlayerSpriteManager.PlayAnimation("skidturn", "walk", nextSpeed: 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                                TwoTailsSpriteManager.PlayAnimation("none");
                            }
                        }
                    }
                    else if(gsp < top) {
                        gsp += acc;
                        if(gsp >= top) {
                            gsp = top;
                        }
                    }
                    if(CheckWallSensors(pushAway: false, updateValues: false)) {
                        if(SensorFCollider != null && horizontalControlLockTimer <= 0) {
                            if(GetState() == State.Walk && PlayerSpriteManager.GetAnimationName() != "push") {
                                PlayerSpriteManager.PlayAnimation("push", 0.75f);
                                TwoTailsSpriteManager.PlayAnimation("spindash", 1.25f);
                            }
                        }
                        else {
                            if(PlayerSpriteManager.GetAnimationName() == "push") {
                                PlayerSpriteManager.PlayAnimation("walk", 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                            }
                        }
                    }
                    else {
                        if(PlayerSpriteManager.GetAnimationName() == "push") {
                            PlayerSpriteManager.PlayAnimation("walk", 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                        }
                    }
                    if(gsp > 0.0f && PlayerSpriteManager.GetAnimationName() != "startskid" && PlayerSpriteManager.GetAnimationName() != "skidturn" && PlayerSpriteManager.GetAnimationName() != "push") {
                        if(10.0f <= Math.Abs(gsp) && PlayerSpriteManager.GetAnimationName() != "dash") {
                            PlayerSpriteManager.PlayAnimationWithOffset("dash");
                        }
                        else if(6.0f <= Math.Abs(gsp) && Math.Abs(gsp) < 10.0f && PlayerSpriteManager.GetAnimationName() != "run") {
                            PlayerSpriteManager.PlayAnimationWithOffset("run");
                        }
                        else if(4.5f <= Math.Abs(gsp) && Math.Abs(gsp) < 6.0f && PlayerSpriteManager.GetAnimationName() != "jog") {
                            PlayerSpriteManager.PlayAnimationWithOffset("jog");
                        }
                        else if(Math.Abs(gsp) < 4.5f && PlayerSpriteManager.GetAnimationName() != "walk") {
                            PlayerSpriteManager.PlayAnimationWithOffset("walk");
                        }
                        PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                        TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                        PlayerSpriteManager.SetAnimationAngleSmooth((5.49778714f <= ang || ang <= 0.78539816f ? 0.0f : ang), smoothSpeed: Math.Max(7.5f, Math.Abs(gsp) * 2.0f));
                        PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                        TwoTailsSpriteManager.PlayAnimation("none");
                    }
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Down, PlayerInputManager.Input.Up) && 1.0f <= Math.Abs(gsp)) {
                    TwoTailsSpriteManager.FlipAnimation(gsp > 0.0f ? true : false);
                    SFXManager.PlayOneShotSFX(SFXManager.spinSFX);
                    SetState(State.Spin);
                }
                else {
                    if(horizontalControlLockTimer > 0) {
                        horizontalControlLockTimer--;
                    }
                    gsp -= (Math.Min(Math.Abs(gsp), frc) * Math.Sign(gsp)) + (slp * (float)Math.Sin(ang));
                    if(Math.Abs(gsp) < 0.5f && (5.49778714f < ang || ang < 0.78539816f)) {
                        gsp = 0.0f;
                        PlayerSpriteManager.PlayAnimation("stand");
                        TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                        SetState(State.Stand);
                    }
                    else if(PlayerSpriteManager.GetAnimationName() != "startskid" && PlayerSpriteManager.GetAnimationName() != "skidturn") {
                        if(10.0f <= Math.Abs(gsp) && PlayerSpriteManager.GetAnimationName() != "dash") {
                            PlayerSpriteManager.PlayAnimationWithOffset("dash");
                        }
                        else if(6.0f <= Math.Abs(gsp) && Math.Abs(gsp) < 10.0f && PlayerSpriteManager.GetAnimationName() != "run") {
                            PlayerSpriteManager.PlayAnimationWithOffset("run");
                        }
                        else if(4.5f <= Math.Abs(gsp) && Math.Abs(gsp) < 6.0f && PlayerSpriteManager.GetAnimationName() != "jog") {
                            PlayerSpriteManager.PlayAnimationWithOffset("jog");
                        }
                        else if(Math.Abs(gsp) < 4.5f && PlayerSpriteManager.GetAnimationName() != "walk") {
                            PlayerSpriteManager.PlayAnimationWithOffset("walk");
                        }
                        PlayerSpriteManager.SetAnimationAngleSmooth((5.49778714f <= ang || ang <= 0.78539816f ? 0.0f : ang), smoothSpeed: Math.Max(7.5f, Math.Abs(gsp) * 2.0f));
                        PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                        TwoTailsSpriteManager.PlayAnimation("none");
                    }
                }
                if(PlayerSpriteManager.GetAnimationName() == "startskid" || PlayerSpriteManager.GetAnimationName() == "skid") {
                    if(skiddustAnimationTimer >= 3) {
                        skiddustAnimationTimer = 0;
                        GameObject particleSkiddust = Instantiate(particleSkiddustPrefab,
                        transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyHeightRadius)),
                        transform.rotation
                        );
                        particleSkiddust.GetComponent<SpriteManager>().SetAnimationSpeed(2.0f);
                        particleSkiddust.GetComponent<SpriteManager>().FlipAnimation(isFacingRight);
                        particleSkiddust.GetComponent<SpriteManager>().SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
                        particleSkiddust.GetComponent<SpriteManager>().destroyAfterAnimation = true;
                    }
                    else {
                        skiddustAnimationTimer++;
                    }
                }
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        if(PlayerSpriteManager.GetAnimationName() == "walk" || PlayerSpriteManager.GetAnimationName() == "jog") {
                            PlayerSpriteManager.PlayAnimationWithOffset("airwalk", speed: 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                            TwoTailsSpriteManager.PlayAnimation("none");
                        }
                        SetState(State.Airwalk);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        if(PlayerSpriteManager.GetAnimationName() == "walk" || PlayerSpriteManager.GetAnimationName() == "jog") {
                            PlayerSpriteManager.PlayAnimationWithOffset("airwalk", speed: 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                            TwoTailsSpriteManager.PlayAnimation("none");
                        }
                        SetState(State.Airwalk);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Spin:
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    CheckCeilingSensors(pushAway: false, updateValues: false);
                    if(SensorECollider == null || SensorFCollider == null) {
                        xsp -= jmp * (float)Math.Sin(ang);
                        ysp -= jmp * (float)Math.Cos(ang);
                        PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                        TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                        SFXManager.PlayOneShotSFX(SFXManager.jumpSFX);
                        SetState(State.Jump);
                    }
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    if(gsp > 0.0f) {
                        gsp -= rolldec + rollfrc;
                        gsp -= (Math.Sign(gsp) == Math.Sign(Math.Sin(ang)) ? slprollup : slprolldown) * (float)Math.Sin(ang);
                        if(gsp <= 0.5f) {
                            if(5.49778714f < ang || ang < 0.78539816f) {
                                SetState(State.Walk);
                            }
                            else {
                                PlayerSpriteManager.PlayAnimation("stand");
                                TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                                SetState(State.Stand);
                            }
                        }
                        else {
                            PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        }
                    }
                    else {
                        gsp += rollfrc;
                        gsp -= (Math.Sign(gsp) == Math.Sign(Math.Sin(ang)) ? slprollup : slprolldown) * (float)Math.Sin(ang);
                        if(gsp >= -0.5f) {
                            if(5.49778714f < ang || ang < 0.78539816f) {
                                SetState(State.Walk);
                            }
                            else {
                                PlayerSpriteManager.PlayAnimation("stand");
                                TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                                SetState(State.Stand);
                            }
                        }
                        else {
                            PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        }
                    }
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    if(gsp < 0.0f) {
                        gsp -= (Math.Sign(gsp) == Math.Sign(Math.Sin(ang)) ? slprollup : slprolldown) * (float)Math.Sin(ang);
                        gsp += rolldec + rollfrc;
                        if(gsp >= -0.5f) {
                            if(5.49778714f < ang || ang < 0.78539816f) {
                                SetState(State.Walk);
                            }
                            else {
                                PlayerSpriteManager.PlayAnimation("stand");
                                TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                                SetState(State.Stand);
                            }
                        }
                        else {
                            PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        }
                    }
                    else {
                        gsp -= rollfrc;
                        gsp -= (Math.Sign(gsp) == Math.Sign(Math.Sin(ang)) ? slprollup : slprolldown) * (float)Math.Sin(ang);
                        if(gsp <= 0.5f) {
                            if(5.49778714f < ang || ang < 0.78539816f) {
                                SetState(State.Walk);
                            }
                            else {
                                PlayerSpriteManager.PlayAnimation("stand");
                                TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                                SetState(State.Stand);
                            }
                        }
                        else {
                            PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        }
                    }
                }
                else {
                    if(gsp > 0.0f) {
                        gsp -= rollfrc;
                        gsp -= (Math.Sign(gsp) == Math.Sign(Math.Sin(ang)) ? slprollup : slprolldown) * (float)Math.Sin(ang);
                        if(gsp <= 0.5f) {
                            if(5.49778714f < ang || ang < 0.78539816f) {
                                SetState(State.Walk);
                            }
                            else {
                                PlayerSpriteManager.PlayAnimation("stand");
                                TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                                SetState(State.Stand);
                            }
                        }
                        else {
                            PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        }
                    }
                    else {
                        gsp += rollfrc;
                        gsp -= (Math.Sign(gsp) == Math.Sign(Math.Sin(ang)) ? slprollup : slprolldown) * (float)Math.Sin(ang);
                        if(gsp >= -0.5f) {
                            if(5.49778714f < ang || ang < 0.78539816f) {
                                SetState(State.Walk);
                            }
                            else {
                                PlayerSpriteManager.PlayAnimation("stand");
                                TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                                SetState(State.Stand);
                            }
                        }
                        else {
                            PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                        }
                    }
                }
                if(gsp > rolltop) {
                    gsp = rolltop;
                }
                else if(gsp < -rolltop) {
                    gsp = -rolltop;
                }
                if(5.49778714f <= ang || ang <= 0.78539816f) {
                    TwoTailsSpriteManager.SetAnimationAngleSmooth(0.0f, smoothSpeed: Math.Max(7.5f, Math.Abs(gsp) * 2.0f));
                }
                else {
                    TwoTailsSpriteManager.SetAnimationAngleSmooth(
                        (ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (gsp > 0.0f ? -1.57079633f : 1.57079633f)),
                        smoothSpeed: Math.Max(7.5f, Math.Abs(gsp) * 2.0f)
                    );
                }
                CheckWallSensors();
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        SetState(State.Airspin);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        SetState(State.Airspin);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Spindash:
                if(!InputManager.GetInput(PlayerInputManager.Input.Down, PlayerInputManager.Input.Up) && !InputManager.GetInput(PlayerInputManager.Input.Spin)) {
                    if(isFacingRight) {
                        gsp = 8.0f + ((float)Math.Floor(spinrev) / 2.0f);
                    }
                    else {
                        gsp = -(8.0f + ((float)Math.Floor(spinrev) / 2.0f));
                    }
                    TwoTailsSpriteManager.FlipAnimation(gsp > 0.0f ? true : false);
                    SFXManager.PlaySFX(SFXManager.dashreleaseSFX);
                    SetState(State.Spin);
                }
                else if(InputManager.GetInputDown(PlayerInputManager.Input.Jump) || InputManager.GetInputDown(PlayerInputManager.Input.Spin)) {
                    spinrev += 2.0f;
                    if(spinrev > 8.0f) {
                        spinrev = 8.0f;
                    }
                    if(spindashSFXCounter < 12) {
                        spindashSFXCounter++;
                    }
                    PlayerSpriteManager.PlayAnimation("spindash", speed: 3.0f);
                    SFXManager.PlaySFX(SFXManager.spindashSFX, pitch: 1.0f + (spindashSFXCounter * 0.0697f));
                }
                else {
                    spinrev -= ((spinrev / 0.125f) - (spinrev % 0.125f)) / 256.0f;
                }
                CheckWallSensors();
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        SetState(State.Airspin);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        SetState(State.Airspin);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Jump:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp -= air;
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp += air;
                }
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    if(dropdashTimer < 0) {
                        dropdashTimer = 0;
                    }
                    if(GetCharacter() == Character.Sonic) {
                        if(GetShield() == Shield.Bubble && ysp > -4.0f) {
                            xsp = 0.0f;
                            ysp = 8.0f;
                            ShieldSpriteManager.PlayAnimation("bubblebounce", "bubble", speed: 2.875f, nextSpeed: 2.125f);
                            Shield2SpriteManager.PlayAnimation("bubblebounceshimmer", "bubbleshimmer", speed: 1.625f, nextSpeed: 1.625f);
                            SFXManager.PlayOneShotSFX(SFXManager.bubblebounceSFX);
                            SetState(State.Bounce);
                        }
                        else if(GetShield() == Shield.Fire) {
                            xsp = isFacingRight ? 8.0f : -8.0f;
                            ysp = 0.0f;
                            ShieldSpriteManager.PlayAnimation((isFacingRight ? "firedashright" : "firedashleft"), "fire", speed: 2.5f, nextSpeed: 5.0f);
                            SFXManager.PlayOneShotSFX(SFXManager.firedashSFX);
                            SetState(State.Airspin);
                        }
                        else if(GetShield() == Shield.Lightning) {
                            ysp = -5.5f;
                            GameObject particleLightningjumpsparks = Instantiate(particleLightningjumpsparksPrefab,
                            transform.position,
                            transform.rotation
                            );
                            particleLightningjumpsparks.GetComponent<SpriteManager>().SetAnimationSpeed(5.0f);
                            particleLightningjumpsparks.GetComponent<SpriteManager>().FlipAnimation(isFacingRight);
                            particleLightningjumpsparks.GetComponent<SpriteManager>().SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
                            particleLightningjumpsparks.GetComponent<SpriteManager>().destroyAfterAnimation = true;
                            SFXManager.PlayOneShotSFX(SFXManager.lightningjumpSFX);
                            SetState(State.Airspin);
                        }
                        else if(GetShield() == Shield.None && sonicInstaShield && !performedInstaShield) {
                            performedInstaShield = true;
                            GameObject particleInstashield = Instantiate(
                                particleInstashieldPrefab,
                                transform.position,
                                transform.rotation
                            );
                            particleInstashield.transform.parent = gameObject.transform;
                            particleInstashield.GetComponent<SpriteManager>().SetSpriteSet("particle");
                            particleInstashield.GetComponent<SpriteManager>().PlayAnimation("instashield", speed: 5.0f);
                            particleInstashield.GetComponent<SpriteManager>().SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
                            particleInstashield.GetComponent<SpriteManager>().SetSpriteAnchor(PlayerSpriteManager);
                            particleInstashield.GetComponent<SpriteManager>().destroyAfterAnimation = true;
                            SFXManager.PlayOneShotSFX(SFXManager.instashieldSFX);
                        }
                    }
                    if(GetCharacter() == Character.Tails) {
                        SetState(State.Fly);
                    }
                    else if(GetCharacter() == Character.Knuckles) {
                        SetState(State.Glide);
                    }
                }
                if(InputManager.GetInput(PlayerInputManager.Input.Jump)) {
                    if(
                        GetCharacter() == Character.Sonic &&
                        GetShield() != Shield.Bubble &&
                        GetShield() != Shield.Fire &&
                        GetShield() != Shield.Lightning &&
                        !InputManager.GetInput(PlayerInputManager.Input.Spin)) {
                        if(dropdashTimer >= 0) {
                            dropdashTimer++;
                        }
                    }
                }
                else {
                    if(ysp < -4.0f) {
                        if(canPerformShortJump) {
                            ysp = -4.0f;
                        }
                        canPerformShortJump = false;
                    }
                }
                if(InputManager.GetInputDown(PlayerInputManager.Input.Spin)) {
                    if(dropdashTimer < 0) {
                        dropdashTimer = 0;
                    }
                }
                if(InputManager.GetInput(PlayerInputManager.Input.Spin)) {
                    if(GetCharacter() == Character.Sonic) {
                        if(dropdashTimer >= 0) {
                            dropdashTimer++;
                        }
                    }
                }
                if(!InputManager.GetInput(PlayerInputManager.Input.Jump) && !InputManager.GetInput(PlayerInputManager.Input.Spin)) {
                    dropdashTimer = 0;
                    if(PlayerSpriteManager.GetAnimationName() != "spin") {
                        PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                    }
                }
                ysp += grv;
                if(ysp > 16.0f) {
                    ysp = 16.0f;
                }
                if(ysp < 0.0f && ysp > -4.0f) {
                    if(Math.Abs(xsp) >= 0.125f) {
                        xsp -= ((xsp / 0.125f) - (xsp % 0.125f)) / 256.0f;
                    }
                }
                if(dropdashTimer >= 20) {
                    if(!sonicDropDash) {
                        dropdashTimer = 19;
                    }
                    else if(PlayerSpriteManager.GetAnimationName() != "dropdash" && GetState() == State.Jump) {
                        PlayerSpriteManager.PlayAnimation("dropdash", speed: 6.0f);
                        SFXManager.PlayOneShotSFX(SFXManager.dropdashSFX);
                    }
                }
                TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(CheckFloorSensors()) {
                    if(GetState() == State.Bounce) {
                        xsp -= 7.5f * (float)Math.Sin(ang);
                        ysp -= 7.5f * (float)Math.Cos(ang);
                        ShieldSpriteManager.PlayAnimation("bubblebounce", "bubble", speed: 2.875f, nextSpeed: 2.125f);
                        Shield2SpriteManager.PlayAnimation("bubblebounceshimmer", "bubbleshimmer", speed: 1.625f, nextSpeed: 1.625f);
                        SFXManager.PlayOneShotSFX(SFXManager.bubblebounceSFX);
                        SetState(State.Jump);
                        canPerformShortJump = false;
                    }
                    else if(dropdashTimer >= 20) {
                        SetState(State.Spin);
                        gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                        if(isFacingRight) {
                            if(xsp <= 0) {
                                gsp = (gsp / 4.0f) + drpspd;
                            }
                            else {
                                if(6.26573201f <= ang || ang <= 0.01745329f) {
                                    gsp = drpspd;
                                }
                                else {
                                    gsp = (gsp / 2.0f) + drpspd;
                                }
                            }
                        }
                        else {
                            if(xsp >= 0) {
                                gsp = (gsp / 4.0f) - drpspd;
                            }
                            else {
                                if(6.26573201f <= ang || ang <= 0.01745329f) {
                                    gsp = -drpspd;
                                }
                                else {
                                    gsp = (gsp / 2.0f) - drpspd;
                                }
                            }
                        }
                        if(gsp < -drpmax) {
                            gsp = -drpmax;
                        }
                        else if(gsp > drpmax) {
                            gsp = drpmax;
                        }
                        GameObject particleDropdashdust = Instantiate(particleDropdashdustPrefab,
                        transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyHeightRadius)),
                        transform.rotation
                        );
                        particleDropdashdust.GetComponent<SpriteManager>().SetAnimationSpeed(1.25f);
                        particleDropdashdust.GetComponent<SpriteManager>().FlipAnimation(isFacingRight);
                        particleDropdashdust.GetComponent<SpriteManager>().SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
                        particleDropdashdust.GetComponent<SpriteManager>().destroyAfterAnimation = true;
                        SFXManager.PlayOneShotSFX(SFXManager.dashreleaseSFX);
                    }
                    else {
                        if(xsp != 0.0f) {
                            SetState(State.Walk);
                        }
                        else {
                            PlayerSpriteManager.PlayAnimation("stand");
                            TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                            SetState(State.Stand);
                        }
                    }
                }
                if(CheckCeilingSensors()) {
                    if(ang != 0.0f) {
                        SetState(State.Walk);
                    }
                }
                break;

            case State.Airwalk:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp -= air;
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp += air;
                }
                ysp += grv;
                if(ysp > 16.0f) {
                    ysp = 16.0f;
                }
                if(ysp < 0.0f && ysp > -4.0f) {
                    if(Math.Abs(xsp) >= 0.125f) {
                        xsp -= ((xsp / 0.125f) - (xsp % 0.125f)) / 256.0f;
                    }
                }
                if(PlayerSpriteManager.GetAnimationName() == "skid") {
                    PlayerSpriteManager.PlayAnimation("airwalk", 0.75f);
                    TwoTailsSpriteManager.PlayAnimation("none");
                }
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(CheckFloorSensors()) {
                    if(xsp != 0.0f) {
                        SetState(State.Walk);
                    }
                    else {
                        PlayerSpriteManager.PlayAnimation("stand");
                        TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                        SetState(State.Stand);
                    }
                }
                if(CheckCeilingSensors()) {
                    if(ang != 0.0f) {
                        SetState(State.Walk);
                    }
                }
                break;

            case State.Airspin:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp -= air;
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp += air;
                }
                ysp += grv;
                if(ysp > 16.0f) {
                    ysp = 16.0f;
                }
                if(ysp < 0.0f && ysp > -4.0f) {
                    if(Math.Abs(xsp) >= 0.125f) {
                        xsp -= ((xsp / 0.125f) - (xsp % 0.125f)) / 256.0f;
                    }
                }
                TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(CheckFloorSensors()) {
                    if(xsp != 0.0f) {
                        SetState(State.Walk);
                    }
                    else {
                        PlayerSpriteManager.PlayAnimation("stand");
                        TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                        SetState(State.Stand);
                    }
                }
                if(CheckCeilingSensors()) {
                    if(ang != 0.0f) {
                        SetState(State.Walk);
                    }
                }
                break;

            case State.Bounce:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp -= air;
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp += air;
                }
                ysp += grv;
                if(ysp > 16.0f) {
                    ysp = 16.0f;
                }
                if(ysp < 0.0f && ysp > -4.0f) {
                    if(Math.Abs(xsp) >= 0.125f) {
                        xsp -= ((xsp / 0.125f) - (xsp % 0.125f)) / 256.0f;
                    }
                }
                TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(CheckFloorSensors()) {
                    xsp -= 7.5f * (float)Math.Sin(ang);
                    ysp -= 7.5f * (float)Math.Cos(ang);
                    SFXManager.PlayOneShotSFX(SFXManager.bubblebounceSFX);
                    SetState(State.Jump);
                    canPerformShortJump = false;
                }
                if(CheckCeilingSensors()) {
                    if(ang != 0.0f) {
                        SetState(State.Walk);
                    }
                }
                break;

            case State.Peelout:
                if(peeloutTimer < 30) {
                    peeloutTimer++;
                }
                if(10.0f <= Math.Abs((float)peeloutTimer / 2.5f) && PlayerSpriteManager.GetAnimationName() != "peelout") {
                    PlayerSpriteManager.PlayAnimationWithOffset("peelout");
                }
                else if(6.0f <= Math.Abs((float)peeloutTimer / 2.5f) && Math.Abs((float)peeloutTimer / 2.5f) < 10.0f && PlayerSpriteManager.GetAnimationName() != "run") {
                    PlayerSpriteManager.PlayAnimationWithOffset("run");
                }
                else if(4.5f <= Math.Abs((float)peeloutTimer / 2.5f) && Math.Abs((float)peeloutTimer / 2.5f) < 6.0f && PlayerSpriteManager.GetAnimationName() != "jog") {
                    PlayerSpriteManager.PlayAnimationWithOffset("jog");
                }
                else if(Math.Abs((float)peeloutTimer / 2.5f) < 4.5f && PlayerSpriteManager.GetAnimationName() != "walk") {
                    PlayerSpriteManager.PlayAnimationWithOffset("walk");
                }
                PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 8.0f - Math.Abs((float)peeloutTimer / 2.5f)));
                if(!InputManager.GetInput(PlayerInputManager.Input.Up, PlayerInputManager.Input.Down)) {
                    if(peeloutTimer < 15) {
                        PlayerSpriteManager.PlayAnimation("stand");
                        SetState(State.Stand);
                    }
                    else {
                        gsp = ((float)peeloutTimer / 2.5f) * (isFacingRight ? 1.0f : -1.0f);
                        SFXManager.PlayOneShotSFX(SFXManager.peeloutreleaseSFX);
                        SetState(State.Walk);
                    }
                }
                CheckWallSensors();
                if(isGrounded) {
                    xsp = gsp * (float)Math.Cos(ang);
                    ysp = gsp * -(float)Math.Sin(ang);
                }
                UpdatePosition(substeps: 4);
                if(isGrounded) {
                    if(!CheckFloorSensors()) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if(1.57079633f <= ang && ang <= 4.71238898f && Math.Abs(gsp) < fall) {
                        PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                        TwoTailsSpriteManager.PlayAnimation("none");
                        SetState(State.Airwalk);
                    }
                    else if((0.78539816f <= ang && ang < 1.57079633f && gsp < fall && gsp >= -0.5f) ||
                            (4.71238898f < ang && ang <= 5.49778714f && gsp > -fall && gsp <= 0.5f)) {
                        SetState(State.Walk);
                        horizontalControlLockTimer = 30;
                    }
                }
                break;

            case State.Fly:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp -= air;
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp += air;
                }
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    if(ysp >= -1.0f && flyTimer > 0) {
                        flygrv = -0.125f;
                    }
                }
                ysp += flygrv;
                if(ysp < -1.0f) {
                    flygrv = 0.03125f;
                }
                if(ysp > 16.0f) {
                    ysp = 16.0f;
                }
                if(ysp < 0.0f && ysp > -4.0f) {
                    if(Math.Abs(xsp) >= 0.125f) {
                        xsp -= ((xsp / 0.125f) - (xsp % 0.125f)) / 256.0f;
                    }
                }
                if(PlayerSpriteManager.GetAnimationName() == "fly") {
                    PlayerSpriteManager.SetAnimationSpeed(ysp < 0.0f ? 5.0f : 2.5f);
                }
                if(flyTimer > 0) {
                    flyTimer--;
                    if(flyTimer <= 0) {
                        PlayerSpriteManager.PlayAnimation("flytired", speed: 1.5f);
                        SFXManager.PlaySFX(SFXManager.flytiredSFX, loop: true);
                    }
                }
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(CheckFloorSensors()) {
                    if(xsp != 0.0f) {
                        SetState(State.Walk);
                    }
                    else {
                        PlayerSpriteManager.PlayAnimation("stand");
                        TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                        SetState(State.Stand);
                    }
                }
                if(CheckCeilingSensors()) {
                    flygrv = 0.03125f;
                }
                break;

            case State.Glide:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    if(isFacingRight) {
                        PlayerSpriteManager.PlayAnimation("glideturn", "glide", speed: 0.875f, nextSpeed: 1.5f);
                        PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                        TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    }
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    if(!isFacingRight) {
                        PlayerSpriteManager.PlayAnimation("glideturn", "glide", speed: 0.875f, nextSpeed: 1.5f);
                        PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                        TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    }
                }
                if(!isFacingRight) {
                    if(gldang < 3.14159265f) {
                        gldang += 0.04908739f;
                        xsp = (float)Math.Cos(gldang) * (float)Math.Abs(gldspd);
                    }
                    else {
                        gldang = 3.14159265f;
                        gldspd = -(float)Math.Abs(gldspd);
                        gldspd -= 0.015625f;
                        if(gldspd < -gldmax) {
                            gldspd = -gldmax;
                        }
                        xsp = gldspd;
                    }
                }
                else {
                    if(gldang > 0.0f) {
                        gldang -= 0.04908739f;
                        xsp = (float)Math.Cos(gldang) * (float)Math.Abs(gldspd);
                    }
                    else {
                        gldang = 0.0f;
                        gldspd = (float)Math.Abs(gldspd);
                        gldspd += 0.015625f;
                        if(gldspd > gldmax) {
                            gldspd = gldmax;
                        }
                        xsp = gldspd;
                    }
                }
                if(ysp < 0.5f) {
                    ysp += gldgrv;
                    if(ysp > 0.5f) {
                        ysp = gldgrv;
                    }
                }
                else if(ysp > 0.5f) {
                    ysp -= gldgrv;
                    if(ysp < 0.5f) {
                        ysp = gldgrv;
                    }
                }
                if(!InputManager.GetInput(PlayerInputManager.Input.Jump)) {
                    xsp = xsp * 0.25f;
                    PlayerSpriteManager.PlayAnimation("startglidefall", "glidefall", speed: 0.75f);
                    SetState(State.Glidefall);
                }
                UpdatePosition(substeps: 4);
                if(CheckWallSensors()) {
                    if(SensorECollider != null && !isFacingRight && gldang >= 2.74889357f) {
                        float surfAng = (ConvertVector3ToAngle(SensorEHit.normal) + 3.14159265f) % 6.28318531f;
                        if(4.69493569f <= surfAng && surfAng <= 4.72984227f) {
                            SetState(State.Wallcling);
                        }
                        else {
                            xsp = xsp * 0.25f;
                            PlayerSpriteManager.PlayAnimation("startglidefall", "glidefall", speed: 0.75f);
                            SetState(State.Glidefall);
                        }
                    }
                    else if(SensorFCollider != null && isFacingRight && gldang <= 0.39269908f) {
                        float surfAng = (ConvertVector3ToAngle(SensorFHit.normal) + 3.14159265f) % 6.28318531f;
                        if(1.55334303f <= surfAng && surfAng <= 1.58824962f) {
                            SetState(State.Wallcling);
                        }
                        else {
                            xsp = xsp * 0.25f;
                            PlayerSpriteManager.PlayAnimation("startglidefall", "glidefall", speed: 0.75f);
                            SetState(State.Glidefall);
                        }
                    }
                }
                if(CheckFloorSensors()) {
                    if (5.49778714f <= ang || ang <= 0.78539816f) {
                        PlayerSpriteManager.PlayAnimation("startglideslide", "glideslide", speed: 1.0f);
                        SetState(State.Glideslide);
                    }
                    else {
                        SetState(State.Walk);
                    }
                }
                CheckCeilingSensors();
                break;

            case State.Glidefall:
                if(InputManager.GetInput(PlayerInputManager.Input.Left, PlayerInputManager.Input.Right)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = false);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp -= air;
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Right, PlayerInputManager.Input.Left)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = true);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp += air;
                }
                ysp += grv;
                if(ysp > 16.0f) {
                    ysp = 16.0f;
                }
                if(ysp < 0.0f && ysp > -4.0f) {
                    if(Math.Abs(xsp) >= 0.125f) {
                        xsp -= ((xsp / 0.125f) - (xsp % 0.125f)) / 256.0f;
                    }
                }
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(CheckFloorSensors()) {
                    xsp = 0.0f;
                    PlayerSpriteManager.PlayAnimation("glideland", "stand", speed: 1.0f);
                    TwoTailsSpriteManager.PlayAnimation("stand");
                    SFXManager.PlayOneShotSFX(SFXManager.glidelandSFX);
                    SetState(State.Glideland);
                }
                CheckCeilingSensors();
                break;

            case State.Glideslide:
                if(gsp > 0.0f) {
                    gsp -= 0.125f;
                    if(gsp <= 0.0f) {
                        PlayerSpriteManager.PlayAnimation("endglideslide", "stand", speed: 1.0f);
                        TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                        SetState(State.Glideland);
                    }
                }
                else if(gsp < 0.0f) {
                    gsp += 0.125f;
                    if(gsp >= 0.0f) {
                        PlayerSpriteManager.PlayAnimation("endglideslide", "stand", speed: 1.0f);
                        TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                        SetState(State.Glideland);
                    }
                }
                else {
                    PlayerSpriteManager.PlayAnimation("endglideslide", "stand", speed: 1.0f);
                    TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                    SetState(State.Glideland);
                }
                if(!InputManager.GetInput(PlayerInputManager.Input.Jump)) {
                    PlayerSpriteManager.PlayAnimation("endglideslide", "stand", speed: 1.0f);
                    TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                    SetState(State.Glideland);
                }
                if(glidedustAnimationTimer >= 8) {
                    glidedustAnimationTimer = 0;
                    GameObject particleSkiddust = Instantiate(particleSkiddustPrefab,
                    transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyHeightRadius)),
                    transform.rotation
                );
                    particleSkiddust.GetComponent<SpriteManager>().SetAnimationSpeed(2.0f);
                    particleSkiddust.GetComponent<SpriteManager>().FlipAnimation(isFacingRight);
                    particleSkiddust.GetComponent<SpriteManager>().SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
                    particleSkiddust.GetComponent<SpriteManager>().destroyAfterAnimation = true;
                    SFXManager.PlayOneShotSFX(SFXManager.glideslideSFX);
                }
                else {
                    glidedustAnimationTimer++;
                }
                xsp = gsp * (float)Math.Cos(ang);
                ysp = gsp * -(float)Math.Sin(ang);
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(!CheckFloorSensors()) {
                    PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                    TwoTailsSpriteManager.PlayAnimation("none");
                    PlayerSpriteManager.PlayAnimation("startglidefall", "glidefall", speed: 0.75f);
                    SetState(State.Glidefall);
                }
                break;

            case State.Glideland:
                glidelandTimer++;
                if(glidelandTimer >= 15) {
                    SetState(State.Stand);

                }
                xsp = gsp * (float)Math.Cos(ang);
                ysp = gsp * -(float)Math.Sin(ang);
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(!CheckFloorSensors()) {
                    PlayerSpriteManager.PlayAnimation("airwalk", speed: 0.75f);
                    TwoTailsSpriteManager.PlayAnimation("none");
                    SetState(State.Airwalk);
                }
                break;

            case State.Wallcling:
                if(InputManager.GetInput(PlayerInputManager.Input.Up, PlayerInputManager.Input.Down)) {
                    if(!CheckCeilingSensors(pushAway: false, updateValues: false)) {
                        ysp = -1.0f;
                        if(PlayerSpriteManager.GetAnimationName() != "wallclimbup") {
                            PlayerSpriteManager.PlayAnimation("wallclimbup", speed: 1.0f);
                        }
                    }
                    else {
                        ysp = 0.0f;
                        if(PlayerSpriteManager.GetAnimationName() != "wallcling") {
                            PlayerSpriteManager.PlayAnimation("wallcling", speed: 0.75f);
                        }
                    }
                }
                else if(InputManager.GetInput(PlayerInputManager.Input.Down, PlayerInputManager.Input.Up)) {
                    ysp = 1.0f;
                    if(PlayerSpriteManager.GetAnimationName() != "wallclimbdown") {
                        PlayerSpriteManager.PlayAnimation("wallclimbdown", speed: 1.25f);
                    }
                }
                else {
                    ysp = 0.0f;
                    if(PlayerSpriteManager.GetAnimationName() != "wallcling") {
                        PlayerSpriteManager.PlayAnimation("wallcling", speed: 0.75f);
                    }
                }
                if(InputManager.GetInputDown(PlayerInputManager.Input.Jump)) {
                    PlayerSpriteManager.FlipAnimation(isFacingRight = !isFacingRight);
                    TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                    xsp = (isFacingRight ? 4.0f : -4.0f);
                    ysp = -4.0f;
                    PlayerSpriteManager.PlayAnimation("spin", speed: 1.5f);
                    TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                    TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                    SFXManager.PlayOneShotSFX(SFXManager.jumpSFX);
                    SetState(State.Jump);
                }
                UpdatePosition(substeps: 4);
                CheckWallSensors();
                if(SensorECollider == null) {
                    if(!isFacingRight && GetState() == State.Wallcling) {
                        if(InputManager.GetInput(PlayerInputManager.Input.Up, PlayerInputManager.Input.Down)) {
                            PlayerSpriteManager.PlayAnimation("wallmount", "stand", speed: 1.35f);
                            SetState(State.Wallmount);
                        }
                        else {
                            PlayerSpriteManager.PlayAnimation("wallfall", "glidefall", speed: 0.75f);
                            SetState(State.Glidefall);
                        }
                    }
                }
                else {
                    if(!isFacingRight && GetState() == State.Wallcling) {
                        float surfAng = (ConvertVector3ToAngle(SensorEHit.normal) + 3.14159265f) % 6.28318531f;
                        if(!(4.69493569f <= surfAng && surfAng <= 4.72984227f)) {
                            PlayerSpriteManager.PlayAnimation("wallfall", "glidefall", speed: 0.75f);
                            SetState(State.Glidefall);
                        }
                    }
                }
                if(SensorFCollider == null) {
                    if(isFacingRight && GetState() == State.Wallcling) {
                        if(InputManager.GetInput(PlayerInputManager.Input.Up, PlayerInputManager.Input.Down)) {
                            PlayerSpriteManager.PlayAnimation("wallmount", "stand", speed: 1.35f);
                            SetState(State.Wallmount);
                        }
                        else {
                            PlayerSpriteManager.PlayAnimation("wallfall", "glidefall", speed: 0.75f);
                            SetState(State.Glidefall);
                        }
                    }
                }
                else {
                    if(isFacingRight && GetState() == State.Wallcling) {
                        float surfAng = (ConvertVector3ToAngle(SensorFHit.normal) + 3.14159265f) % 6.28318531f;
                        if(!(1.55334303f <= surfAng && surfAng <= 1.58824962f)) {
                            PlayerSpriteManager.PlayAnimation("wallfall", "glidefall", speed: 0.75f);
                            SetState(State.Glidefall);
                        }
                    }
                }
                if(CheckFloorSensors() && GetState() == State.Wallcling) {
                    PlayerSpriteManager.PlayAnimation("stand");
                    TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                    SetState(State.Stand);
                }
                CheckCeilingSensors(updateValues: false);
                ang = 0.0f;
                break;

            case State.Wallmount:
                glidelandTimer++;
                if(glidelandTimer <= 12) {
                    xsp = 0.0f;
                    ysp = -1.5f * -0.64f / yScale;
                    PlayerSpriteManager.transform.position += ConvertVector2ToVector3(new Vector2(0.0f, -0.01666667f));
                }
                else {
                    xsp = (isFacingRight ? 1.5625f : -1.5625f) * 0.64f / xScale;
                    ysp = 0.0f;
                    PlayerSpriteManager.transform.localPosition = PlayerSpriteManager.transform.localPosition * ((float)(24 - glidelandTimer)/(float)(25 - glidelandTimer));
                }
                if(glidelandTimer >= 24) {
                    SetState(State.Stand);
                }
                UpdatePosition(substeps: 4);
                CheckWallSensors(updateValues: false);
                CheckFloorSensors(updateValues: false);
                CheckCeilingSensors(updateValues: false);
                break;

            default:
                break;
        }
    }

    public void EnterState(State state) {
        switch(state) {
            case State.Stand:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = true;
                gsp = 0.0f;
                idleAnimationTimer = 0;
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                TwoTailsSpriteManager.SetAnimationAngle(0.0f);
                break;
            case State.Lookup:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = true;
                gsp = 0.0f;
                PlayerSpriteManager.PlayAnimation("startlookup", "lookup", speed: 2.0f);
                if(TwoTailsSpriteManager.GetAnimationName() != "stand") {
                    TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                }
                break;
            case State.Crouch:
                SetSize(0.09f, 0.14f, 0.1f);
                isGrounded = true;
                gsp = 0.0f;
                PlayerSpriteManager.PlayAnimation("startcrouch", "crouch", speed: 2.0f);
                if(TwoTailsSpriteManager.GetAnimationName() != "stand") {
                    TwoTailsSpriteManager.PlayAnimation("stand", speed: 1.25f);
                }
                break;
            case State.Walk:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = true;
                if(10.0f <= Math.Abs(gsp) && PlayerSpriteManager.GetAnimationName() != "dash") {
                    PlayerSpriteManager.PlayAnimationWithOffset("dash");
                }
                else if(6.0f <= Math.Abs(gsp) && Math.Abs(gsp) < 10.0f && PlayerSpriteManager.GetAnimationName() != "run") {
                    PlayerSpriteManager.PlayAnimationWithOffset("run");
                }
                else if(4.5f <= Math.Abs(gsp) && Math.Abs(gsp) < 6.0f && PlayerSpriteManager.GetAnimationName() != "jog") {
                    PlayerSpriteManager.PlayAnimationWithOffset("jog");
                }
                else if(Math.Abs(gsp) < 4.5f && PlayerSpriteManager.GetAnimationName() != "walk") {
                    PlayerSpriteManager.PlayAnimationWithOffset("walk");
                }
                PlayerSpriteManager.SetAnimationSpeed(6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                TwoTailsSpriteManager.PlayAnimation("none");
                if((0.78539816f <= ang && ang <= 1.57079633f) || (4.71238898f <= ang && ang <= 5.49778714f)) {
                    PlayerSpriteManager.SetAnimationAngleSmooth(5.49778714f <= ang || ang <= 0.78539816f ? 0.0f : ang);
                }
                else {
                    PlayerSpriteManager.SetAnimationAngle(5.49778714f <= ang || ang <= 0.78539816f ? 0.0f : ang);
                }
                TwoTailsSpriteManager.SetAnimationAngle(5.49778714f <= ang || ang <= 0.78539816f ? 0.0f : ang);
                break;
            case State.Spin:
                SetSize(0.07f, 0.14f, 0.1f);
                isGrounded = true;
                PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                if(5.49778714f <= ang || ang <= 0.78539816f) {
                    TwoTailsSpriteManager.SetAnimationAngle(0.0f);
                }
                else {
                    TwoTailsSpriteManager.SetAnimationAngle(
                        (ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (gsp > 0.0f ? -1.57079633f : 1.57079633f)));
                }
                break;
            case State.Spindash:
                SetSize(0.09f, 0.14f, 0.1f);
                isGrounded = true;
                spinrev = 0.0f;
                spindashSFXCounter = 0;
                PlayerSpriteManager.PlayAnimation("spindash", speed: 3.0f);
                TwoTailsSpriteManager.PlayAnimation("spindash", 2.5f);
                GameObject particleSpindashdust = Instantiate(
                    particleSpindashdustPrefab,
                    transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyHeightRadius)),
                    transform.rotation
                );
                particleSpindashdust.transform.parent = gameObject.transform;
                particleSpindashdust.GetComponent<SpriteManager>().SetSpriteSet("particle");
                particleSpindashdust.GetComponent<SpriteManager>().PlayAnimation("spindashdust", speed: 6.0f);
                particleSpindashdust.GetComponent<SpriteManager>().SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
                particleSpindashdust.GetComponent<SpriteManager>().SetSpriteAnchor(PlayerSpriteManager);
                SFXManager.PlaySFX(SFXManager.spindashSFX);
                break;
            case State.Jump:
                SetSize(0.07f, 0.14f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                canPerformShortJump = true;
                performedInstaShield = false;
                dropdashTimer = -1;
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                break;
            case State.Airwalk:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                if(PlayerSpriteManager.GetAnimationName() == "walk" || PlayerSpriteManager.GetAnimationName() == "jog") {
                    PlayerSpriteManager.PlayAnimationWithOffset("airwalk", speed: 6.0f / Math.Max(1.0f, 8.0f - Math.Abs(gsp)));
                }
                PlayerSpriteManager.SetAnimationAngleSmooth(0.0f, smoothSpeed: 2.8125f);
                break;
            case State.Airspin:
                SetSize(0.07f, 0.14f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                if(PlayerSpriteManager.GetAnimationName() != "spin") {
                    PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                    TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                }
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                break;
            case State.Bounce:
                SetSize(0.07f, 0.14f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                if(PlayerSpriteManager.GetAnimationName() != "spin") {
                    PlayerSpriteManager.PlayAnimation("spin", speed: 6.0f / Math.Max(1.0f, 4.0f - Math.Abs(gsp)));
                    TwoTailsSpriteManager.PlayAnimation("spin", speed: 1.25f);
                }
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                TwoTailsSpriteManager.SetAnimationAngle((ConvertVector2ToAngle(new Vector2(xsp, -ysp)) + (isFacingRight? -1.57079633f : 1.57079633f)));
                break;
            case State.Peelout:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = true;
                gsp = 0.0f;
                peeloutTimer = 0;
                PlayerSpriteManager.PlayAnimation("walk", speed: 0.75f);
                TwoTailsSpriteManager.PlayAnimation("none");
                SFXManager.PlaySFX(SFXManager.peeloutSFX);
                break;
            case State.Fly:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                flygrv = 0.03125f;
                flyTimer = 480;
                PlayerSpriteManager.PlayAnimation("fly", speed: 2.5f);
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                TwoTailsSpriteManager.PlayAnimation("none");
                TwoTailsSpriteManager.SetAnimationAngle(0.0f);
                SFXManager.PlaySFX(SFXManager.flySFX, loop: true);
                break;
            case State.Glide:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                gldspd = 4.0f * (isFacingRight ? 1.0f : -1.0f);
                if(ysp < 0.0f) {
                    ysp = 0.0f;
                }
                gldang = (isFacingRight ? 0.0f : 3.14159265f);
                PlayerSpriteManager.PlayAnimation("glide", speed: 1.5f);
                PlayerSpriteManager.SetAnimationAngle(0.0f);
                TwoTailsSpriteManager.PlayAnimation("none");
                TwoTailsSpriteManager.SetAnimationAngle(0.0f);
                break;
            case State.Glidefall:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                break;
            case State.Glideslide:
                SetSize(0.09f, 0.14f, 0.1f);
                isGrounded = true;
                glidedustAnimationTimer = 8;
                break;
            case State.Glideland:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = true;
                gsp = 0.0f;
                glidelandTimer = 0;
                TwoTailsSpriteManager.SetAnimationAngle(0.0f);
                break;
            case State.Wallcling:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = false;
                ang = 0.0f;
                xsp = 0.0f;
                ysp = 0.0f;
                PlayerSpriteManager.PlayAnimation("wallcling", speed: 0.75f);
                SFXManager.PlayOneShotSFX(SFXManager.wallclingSFX);
                break;
            case State.Wallmount:
                SetSize(0.09f, 0.19f, 0.1f);
                isGrounded = false;
                glidelandTimer = 0;
                PlayerSpriteManager.transform.localPosition = ConvertVector2ToVector3(new Vector2((isFacingRight ? 0.2f : -0.2f), 0.2f));
                break;
            default:
                break;
        }
    }

    public void LeaveState(State state) {
        Transform[] t;
        switch(state) {
            case State.Walk:
                horizontalControlLockTimer = 0;
                break;
            case State.Spin:
                TwoTailsSpriteManager.FlipAnimation(isFacingRight);
                break;
            case State.Spindash:
                t = gameObject.GetComponentsInChildren<Transform>();
                if(t != null) {
                    foreach(Transform child in t) {
                        if(child.name == particleSpindashdustPrefab.name + "(Clone)") {
                            Destroy(child.gameObject);
                        }
                    }
                }
                break;
            case State.Jump:
                t = gameObject.GetComponentsInChildren<Transform>();
                if(t != null) {
                    foreach(Transform child in t) {
                        if(child.name == particleInstashieldPrefab.name + "(Clone)") {
                            Destroy(child.gameObject);
                        }
                    }
                }
                break;
            case State.Airspin:
                if(GetShield() == Shield.Fire && ShieldSpriteManager.GetAnimationName() != "fire") {
                    ShieldSpriteManager.PlayAnimation("fire", speed: 5.0f);
                }
                break;
            case State.Peelout:
                SFXManager.StopSFX();
                break;
            case State.Fly:
                SFXManager.StopSFX();
                break;
            default:
                break;
        }
    }

    public void SetCharacter(Character character) {
        switch(currentCharacter) {
            case Character.Sonic:
                if(dropdashTimer > 0) {
                    dropdashTimer = 0;
                }
                if(GetState() == State.Peelout) {
                    SetState(State.Stand);
                }
                else if(GetState() == State.Bounce) {
                    SetState(State.Airspin);
                }
                break;
            case Character.Tails:
                if(GetState() == State.Fly) {
                    SetState(State.Airwalk);
                }
                break;
            case Character.Knuckles:
                if(GetState() == State.Glide || GetState() == State.Glidefall || GetState() == State.Wallcling) {
                    SetState(State.Airwalk);
                }
                else if(GetState() == State.Glideslide) {
                    SetState(State.Walk);
                }
                else if(GetState() == State.Glideland || GetState() == State.Wallmount) {
                    SetState(State.Stand);
                }
                break;
            default:
                break;
        }

        currentCharacter = character;

        switch(currentCharacter) {
            case Character.Sonic:
                PlayerSpriteManager.SetSpriteSet("sonic");
                TwoTailsSpriteManager.SetSpriteVisibility(false);
                jmp = 6.5f;
                break;
            case Character.Tails:
                PlayerSpriteManager.SetSpriteSet("tails");
                TwoTailsSpriteManager.SetSpriteVisibility(true);
                jmp = 6.5f;
                break;
            case Character.Knuckles:
                PlayerSpriteManager.SetSpriteSet("knuckles");
                TwoTailsSpriteManager.SetSpriteVisibility(false);
                jmp = (knucklesShortJump ? 6.0f : 6.5f);
                break;
            default:
                break;
        }
        PlayerSpriteManager.PlayAnimationWithOffset(PlayerSpriteManager.GetAnimationName());
    }

    public Character GetCharacter() {
        return currentCharacter;
    }

    public void SetState(State state) {
        LeaveState(currentState);
        currentState = state;
        EnterState(currentState);
    }

    public State GetState() {
        return currentState;
    }

    public void SetShield(Shield shield) {
        currentShield = shield;
        switch(currentShield) {
            case Shield.None:
                ShieldSpriteManager.PlayAnimation("none");
                Shield2SpriteManager.PlayAnimation("none");
                break;
            case Shield.Blue:
                ShieldSpriteManager.PlayAnimation("blue", speed: 1.75f);
                Shield2SpriteManager.PlayAnimation("none");
                SFXManager.PlayOneShotSFX(SFXManager.getblueshieldSFX);
                break;
            case Shield.Bubble:
                ShieldSpriteManager.PlayAnimation("bubble", speed: 2.125f);
                Shield2SpriteManager.spriteRenderer.sortingOrder = 3;
                Shield2SpriteManager.PlayAnimation("bubbleshimmer", speed: 1.625f);
                Shield2SpriteManager.spriteRenderer.sortingOrder = 3;
                SFXManager.PlayOneShotSFX(SFXManager.getbubbleshieldSFX);
                break;
            case Shield.Fire:
                ShieldSpriteManager.PlayAnimation("fire", speed: 5.0f);
                Shield2SpriteManager.PlayAnimation("none");
                SFXManager.PlayOneShotSFX(SFXManager.getfireshieldSFX);
                break;
            case Shield.Lightning:
                ShieldSpriteManager.PlayAnimation("lightning", speed: 2.4375f);
                Shield2SpriteManager.spriteRenderer.sortingOrder = -2;
                Shield2SpriteManager.PlayAnimation("lightningback", speed: 2.4375f);
                Shield2SpriteManager.spriteRenderer.sortingOrder = -2;
                SFXManager.PlayOneShotSFX(SFXManager.getlightningshieldSFX);
                break;
            default:
                break;
        }
    }

    public Shield GetShield() {
        return currentShield;
    }

    public void UpdatePosition(int substeps = 1) {
        for(int i = 0; i < substeps; i++) {
            transform.position += ConvertVector2ToVector3(new Vector2(
                xsp * xScale / (float)substeps / Application.targetFrameRate,
                ysp * yScale / (float)substeps / Application.targetFrameRate
            ));
            if(i < substeps - 1) {
                CheckWallSensors(updateValues: false);
                CheckFloorSensors(updateValues: false);
                if(!isGrounded) {
                    CheckCeilingSensors(updateValues: false);
                }
            }
        }
    }

    public void SetSize(float widthRadius, float heightRadius, float pushRadius) {
        bodyWidthRadius = widthRadius;
        bodyHeightRadius = heightRadius;
        bodyPushRadius = pushRadius;
        PlayerSpriteManager.transform.localPosition = ConvertVector2ToVector3(new Vector2(0.0f, 0.19f - bodyHeightRadius));
        TwoTailsSpriteManager.transform.localPosition = ConvertVector2ToVector3(new Vector2(0.0f, 0.14f - bodyHeightRadius));
        cameraFocus.transform.localPosition = ConvertVector2ToVector3(new Vector2(0.0f, 0.19f - bodyHeightRadius));
    }

    Vector3 ConvertVector2ToVector3(Vector2 vector2) {
        return vector2.x * planeRightVector + vector2.y * planeUpVector;
    }

    Vector2 ConvertVector3ToVector2(Vector3 vector3) {
        return new Vector2(Vector3.Dot(planeRightVector, vector3), Vector3.Dot(planeUpVector, vector3));
    }

    float ConvertVector2ToAngle(Vector2 vector2) {
        return (float)Math.Atan2(vector2.x, -vector2.y);
    }

    float ConvertVector3ToAngle(Vector3 vector3) {
        return ConvertVector2ToAngle(ConvertVector3ToVector2(vector3));
    }

    bool CheckFloorSensors(bool pushAway = true, bool updateValues = true) {
        Vector3 SensorAOrigin;
        Vector3 SensorBOrigin;
        Vector3 SensorADirection;
        Vector3 SensorBDirection;
        bool SensorAActive;
        bool SensorBActive;

        // Floor mode
        if(5.49778714f <= ang || ang <= 0.78539816f) {
            SensorAOrigin = transform.position + ConvertVector2ToVector3(new Vector2(-bodyWidthRadius, 0.0f));
            SensorBOrigin = transform.position + ConvertVector2ToVector3(new Vector2(bodyWidthRadius, 0.0f));
            SensorADirection = -planeUpVector;
            SensorBDirection = -planeUpVector;
        }
        // Right Wall mode
        else if(0.78539816f < ang && ang < 2.35619449f) {
            SensorAOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyWidthRadius));
            SensorBOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, bodyWidthRadius));
            SensorADirection = planeRightVector;
            SensorBDirection = planeRightVector;
        }
        // Ceiling mode
        else if(2.35619449f <= ang && ang <= 3.92699082f) {
            SensorAOrigin = transform.position + ConvertVector2ToVector3(new Vector2(bodyWidthRadius, 0.0f));
            SensorBOrigin = transform.position + ConvertVector2ToVector3(new Vector2(-bodyWidthRadius, 0.0f));
            SensorADirection = planeUpVector;
            SensorBDirection = planeUpVector;
        }
        // Left Wall mode
        else { // (3.92699082f < ang && ang < 5.49778714f)
            SensorAOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, bodyWidthRadius));
            SensorBOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyWidthRadius));
            SensorADirection = -planeRightVector;
            SensorBDirection = -planeRightVector;
        }

        if(isGrounded) {
            SensorAActive = true;
            SensorBActive = true;
        }
        else {
            if(ysp > -0.001f) {
                SensorAActive = true;
                SensorBActive = true;
            }
            else {
                SensorAActive = false;
                SensorBActive = false;
            }
        }

        if(SensorAActive && Physics.Raycast(SensorAOrigin, SensorADirection, out SensorAHit,
                            maxDistance: bodyHeightRadius + (isGrounded ? 0.16f : 0.001f),
                            layerMask: 1 << 8 | (5.49778714f <= ang || ang <= 0.78539816f ? 1 << 9 : 0)) ) {
            SensorACollider = SensorAHit.collider;
        }
        else {
            SensorACollider = null;
        }

        if(SensorBActive && Physics.Raycast(SensorBOrigin, SensorBDirection, out SensorBHit,
                            maxDistance: bodyHeightRadius + (isGrounded ? 0.16f : 0.001f),
                            layerMask: 1 << 8 | (5.49778714f <= ang || ang <= 0.78539816f ? 1 << 9 : 0))) {
            SensorBCollider = SensorBHit.collider;
        }
        else {
            SensorBCollider = null;
        }

        if((SensorACollider != null || SensorBCollider != null)) {
            if(SensorACollider != null && SensorBCollider != null) {
                if(SensorAHit.distance < SensorBHit.distance) {
                    if(pushAway) {
                        transform.position -= SensorADirection * (bodyHeightRadius - SensorAHit.distance);
                    }
                    if(updateValues) {
                        ang = (ConvertVector3ToAngle(SensorAHit.normal) + 3.14159265f) % 6.28318531f;
                    }
                }
                else {
                    if(pushAway) {
                        transform.position -= SensorBDirection * (bodyHeightRadius - SensorBHit.distance);
                    }
                    if(updateValues) {
                        ang = (ConvertVector3ToAngle(SensorBHit.normal) + 3.14159265f) % 6.28318531f;
                    }
                }
            }
            else if(SensorACollider != null) {
                if(pushAway) {
                    transform.position -= SensorADirection * (bodyHeightRadius - SensorAHit.distance);
                }
                if(updateValues) {
                    ang = (ConvertVector3ToAngle(SensorAHit.normal) + 3.14159265f) % 6.28318531f;
                }
            }
            else {
                if(pushAway) {
                    transform.position -= SensorBDirection * (bodyHeightRadius - SensorBHit.distance);
                }
                if(updateValues) {
                    ang = (ConvertVector3ToAngle(SensorBHit.normal) + 3.14159265f) % 6.28318531f;
                }
            }
            if(updateValues && !isGrounded) {
                gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                xsp = gsp * (float)Math.Cos(ang);
                ysp = gsp * -(float)Math.Sin(ang);
            }
            return true;
        }
        if(updateValues) {
            ang = 0.0f;
        }
        return false;
    }

    bool CheckWallSensors(bool pushAway = true, bool updateValues = true) {
        Vector3 SensorEOrigin;
        Vector3 SensorFOrigin;
        Vector3 SensorEDirection;
        Vector3 SensorFDirection;
        bool SensorEActive;
        bool SensorFActive;

        SensorEOrigin = transform.position + ConvertVector2ToVector3(Vector2.zero);
        SensorFOrigin = transform.position + ConvertVector2ToVector3(Vector2.zero);

        // Floor mode
        if(5.49778714f <= ang || ang <= 0.78539816f) {
            SensorEDirection = -planeRightVector;
            SensorFDirection = planeRightVector;
        }
        // Right Wall mode
        else if(0.78539816f < ang && ang < 2.35619449f) {
            SensorEDirection = -planeUpVector;
            SensorFDirection = planeUpVector;
        }
        // Ceiling mode
        else if(2.35619449f <= ang && ang <= 3.92699082f) {
            SensorEDirection = planeRightVector;
            SensorFDirection = -planeRightVector;
        }
        // Left Wall mode
        else { // (3.92699082f < ang && ang < 5.49778714f)
            SensorEDirection = planeUpVector;
            SensorFDirection = -planeUpVector;
        }

        if(isGrounded) {
            if(gsp > 0.001f) {
                SensorEActive = false;
                SensorFActive = true;
            }
            else if(gsp < -0.001f) {
                SensorEActive = true;
                SensorFActive = false;
            }
            else {
                SensorEActive = true;
                SensorFActive = true;
            }
        }
        else {
            if(xsp > 0.001f) {
                SensorEActive = false;
                SensorFActive = true;
            }
            else if(xsp < -0.001f) {
                SensorEActive = true;
                SensorFActive = false;
            }
            else {
                SensorEActive = true;
                SensorFActive = true;
            }
        }

        if(SensorEActive && Physics.Raycast(SensorEOrigin, SensorEDirection, out SensorEHit,
                            maxDistance: bodyPushRadius + (GetState() == State.Wallcling ? 0.01f : 0.001f),
                            layerMask: 1 << 8 | (0.78539816f < ang && ang < 2.35619449f ? 1 << 9 : 0))) {
            SensorECollider = SensorEHit.collider;
        }
        else {
            SensorECollider = null;
        }

        if(SensorFActive && Physics.Raycast(SensorFOrigin, SensorFDirection, out SensorFHit,
                            maxDistance: bodyPushRadius + (GetState() == State.Wallcling ? 0.01f : 0.001f),
                            layerMask: 1 << 8 | (3.92699082f < ang && ang < 5.49778714f ? 1 << 9 : 0))) {
            SensorFCollider = SensorFHit.collider;
        }
        else {
            SensorFCollider = null;
        }

        if(SensorECollider != null ^ SensorFCollider != null) {
            if(SensorECollider != null) {
                if(updateValues) {
                    ang = (ConvertVector3ToAngle(SensorAHit.normal) + 3.14159265f) % 6.28318531f;
                }
                if(pushAway) {
                    transform.position -= SensorEDirection * (bodyPushRadius - SensorEHit.distance);
                }
                if(updateValues) {
                    if(isGrounded) {
                        if(0.78539816f < ang && ang < 2.35619449f) {
                            ang = (ConvertVector3ToAngle(SensorEHit.normal) + 3.14159265f) % 6.28318531f;
                            gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                            xsp = gsp * (float)Math.Cos(ang);
                            ysp = gsp * -(float)Math.Sin(ang);
                        }
                        else if(gsp < 0.0f) {
                            gsp = 0.0f;
                        }
                    }
                    else if(xsp < 0.0f) {
                        float surfAng = (ConvertVector3ToAngle(SensorEHit.normal) + 3.14159265f) % 6.28318531f;
                        float spd = xsp * (float)Math.Cos(surfAng) + ysp * -(float)Math.Sin(surfAng);
                        xsp = spd * (float)Math.Cos(surfAng);
                        ysp = spd * -(float)Math.Sin(surfAng);
                    }
                }
            }
            else {
                if(pushAway) {
                    transform.position -= SensorFDirection * (bodyPushRadius - SensorFHit.distance);
                }
                if(updateValues) {
                    if(isGrounded) {
                        if(3.92699082f < ang && ang < 5.49778714f) {
                            ang = (ConvertVector3ToAngle(SensorFHit.normal) + 3.14159265f) % 6.28318531f;
                            gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                            xsp = gsp * (float)Math.Cos(ang);
                            ysp = gsp * -(float)Math.Sin(ang);
                        }
                        else if(gsp > 0.0f) {
                            gsp = 0.0f;
                        }
                    }
                    else if(xsp > 0.0f) {
                        float surfAng = (ConvertVector3ToAngle(SensorFHit.normal) + 3.14159265f) % 6.28318531f;
                        float spd = xsp * (float)Math.Cos(surfAng) + ysp * -(float)Math.Sin(surfAng);
                        xsp = spd * (float)Math.Cos(surfAng);
                        ysp = spd * -(float)Math.Sin(surfAng);
                    }
                }
            }
            return true;
        }
        return false;
    }

    bool CheckCeilingSensors(bool pushAway = true, bool updateValues = true) {
        Vector3 SensorCOrigin;
        Vector3 SensorDOrigin;
        Vector3 SensorCDirection;
        Vector3 SensorDDirection;
        bool SensorCActive;
        bool SensorDActive;

        // Floor mode
        if(5.49778714f <= ang || ang <= 0.78539816f) {
            SensorCOrigin = transform.position + ConvertVector2ToVector3(new Vector2(-bodyWidthRadius, 0.0f));
            SensorDOrigin = transform.position + ConvertVector2ToVector3(new Vector2(bodyWidthRadius, 0.0f));
            SensorCDirection = planeUpVector;
            SensorDDirection = planeUpVector;
        }
        // Right Wall mode
        else if(0.78539816f < ang && ang < 2.35619449f) {
            SensorCOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyWidthRadius));
            SensorDOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, bodyWidthRadius));
            SensorCDirection = -planeRightVector;
            SensorDDirection = -planeRightVector;
        }
        // Ceiling mode
        else if(2.35619449f <= ang && ang <= 3.92699082f) {
            SensorCOrigin = transform.position + ConvertVector2ToVector3(new Vector2(bodyWidthRadius, 0.0f));
            SensorDOrigin = transform.position + ConvertVector2ToVector3(new Vector2(-bodyWidthRadius, 0.0f));
            SensorCDirection = -planeUpVector;
            SensorDDirection = -planeUpVector;
        }
        // Left Wall mode
        else { // (3.92699082f < ang && ang < 5.49778714f)
            SensorCOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, bodyWidthRadius));
            SensorDOrigin = transform.position + ConvertVector2ToVector3(new Vector2(0.0f, -bodyWidthRadius));
            SensorCDirection = planeRightVector;
            SensorDDirection = planeRightVector;
        }

        if(isGrounded) {
            SensorCActive = true;
            SensorDActive = true;
        }
        else {
            if(ysp > 0.001f) {
                SensorCActive = false;
                SensorDActive = false;
            }
            else {
                SensorCActive = true;
                SensorDActive = true;
            }
        }

        if(SensorCActive && Physics.Raycast(SensorCOrigin, SensorCDirection, out SensorCHit,
                            maxDistance: bodyHeightRadius + 0.001f,
                            layerMask: 1 << 8 | (2.35619449f <= ang && ang <= 3.92699082f ? 1 << 9 : 0))) {
            SensorCCollider = SensorCHit.collider;
        }
        else {
            SensorCCollider = null;
        }

        if(SensorDActive && Physics.Raycast(SensorDOrigin, SensorDDirection, out SensorDHit,
                            maxDistance: bodyHeightRadius + 0.001f,
                            layerMask: 1 << 8 | (2.35619449f <= ang && ang <= 3.92699082f ? 1 << 9 : 0))) {
            SensorDCollider = SensorDHit.collider;
        }
        else {
            SensorDCollider = null;
        }

        if(SensorCCollider != null || SensorDCollider != null && (ysp <= 0.0f)) {
            if(SensorCCollider != null && SensorDCollider != null) {
                if(SensorCHit.distance < SensorDHit.distance) {
                    if(pushAway) {
                        transform.position -= SensorCDirection * (bodyHeightRadius - SensorCHit.distance);
                    }
                    if(updateValues && ysp < 0.0f) {
                        float surfAng = (ConvertVector3ToAngle(SensorCHit.normal) + 3.14159265f) % 6.28318531f;
                        if((1.57079632f < surfAng && surfAng <= 2.35619449f) || (3.92699082f <= surfAng && surfAng < 4.71238898f)) {
                            ang = surfAng;
                            gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                            xsp = gsp * (float)Math.Cos(ang);
                            ysp = gsp * -(float)Math.Sin(ang);
                        }
                        else {
                            float spd = xsp * (float)Math.Cos(surfAng) + ysp * -(float)Math.Sin(surfAng);
                            xsp = spd * (float)Math.Cos(surfAng);
                            ysp = spd * -(float)Math.Sin(surfAng);
                        }
                    }
                }
                else {
                    if(pushAway) {
                        transform.position -= SensorDDirection * (bodyHeightRadius - SensorDHit.distance);
                    }
                    if(updateValues && ysp < 0.0f) {
                        float surfAng = (ConvertVector3ToAngle(SensorDHit.normal) + 3.14159265f) % 6.28318531f;
                        if((1.57079632f < surfAng && surfAng <= 2.35619449f) || (3.92699082f <= surfAng && surfAng < 4.71238898f)) {
                            ang = surfAng;
                            gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                            xsp = gsp * (float)Math.Cos(ang);
                            ysp = gsp * -(float)Math.Sin(ang);
                        }
                        else {
                            float spd = xsp * (float)Math.Cos(surfAng) + ysp * -(float)Math.Sin(surfAng);
                            xsp = spd * (float)Math.Cos(surfAng);
                            ysp = spd * -(float)Math.Sin(surfAng);
                        }
                    }
                }
            }
            else if(SensorCCollider != null) {
                if(pushAway) {
                    transform.position -= SensorCDirection * (bodyHeightRadius - SensorCHit.distance);
                }
                if(updateValues && ysp < 0.0f) {
                        float surfAng = (ConvertVector3ToAngle(SensorCHit.normal) + 3.14159265f) % 6.28318531f;
                        if((1.57079632f < surfAng && surfAng <= 2.35619449f) || (3.92699082f <= surfAng && surfAng < 4.71238898f)) {
                            ang = surfAng;
                            gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                            xsp = gsp * (float)Math.Cos(ang);
                            ysp = gsp * -(float)Math.Sin(ang);
                        }
                        else {
                            float spd = xsp * (float)Math.Cos(surfAng) + ysp * -(float)Math.Sin(surfAng);
                            xsp = spd * (float)Math.Cos(surfAng);
                            ysp = spd * -(float)Math.Sin(surfAng);
                        }
                }
            }
            else {
                if(pushAway) {
                    transform.position -= SensorDDirection * (bodyHeightRadius - SensorDHit.distance);
                }
                if(updateValues && ysp < 0.0f) {
                    float surfAng = (ConvertVector3ToAngle(SensorDHit.normal) + 3.14159265f) % 6.28318531f;
                    if((1.57079632f < surfAng && surfAng <= 2.35619449f) || (3.92699082f <= surfAng && surfAng < 4.71238898f)) {
                        ang = surfAng;
                        gsp = xsp * (float)Math.Cos(ang) + ysp * -(float)Math.Sin(ang);
                        xsp = gsp * (float)Math.Cos(ang);
                        ysp = gsp * -(float)Math.Sin(ang);
                    }
                    else {
                        float spd = xsp * (float)Math.Cos(surfAng) + ysp * -(float)Math.Sin(surfAng);
                        xsp = spd * (float)Math.Cos(surfAng);
                        ysp = spd * -(float)Math.Sin(surfAng);
                    }
                }
            }
            return true;
        }
        return false;
    }

    public void SetPlane(Vector3 upVector, Vector3 rightVector, Vector3 position, float alignmentSpeed = -1.0f) {
        planeUpVector = upVector.normalized;
        planeRightVector = rightVector.normalized;
        planePosition = position;
        planeAlignmentSpeed = alignmentSpeed;
        if(planeAlignmentSpeed < 0.0f) {
            transform.position = new Plane(Vector3.Cross(planeUpVector, planeRightVector), planePosition).ClosestPointOnPlane(transform.position);
        }
        PlayerSpriteManager.SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
        TwoTailsSpriteManager.SetNormalVector(Vector3.Cross(planeUpVector, planeRightVector));
    }

    public Vector3 GetPlaneUpVector() {
        return planeUpVector;
    }

    public Vector3 GetPlaneRightVector() {
        return planeRightVector;
    }

    public void UpdatePlane() {
        Vector3 distanceVector = new Plane(Vector3.Cross(planeUpVector, planeRightVector), planePosition).ClosestPointOnPlane(transform.position) - transform.position;
        if(distanceVector.magnitude > planeAlignmentSpeed / Application.targetFrameRate) {
            transform.position += distanceVector.normalized * planeAlignmentSpeed / Application.targetFrameRate;
        }
        else {
            transform.position += distanceVector;
            planeAlignmentSpeed = -1.0f;
        }
    }
}