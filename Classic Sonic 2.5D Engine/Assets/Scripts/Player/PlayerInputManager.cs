using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {

    public enum Input: int {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Jump = 4,
        Spin = 5,
        Super = 6,
        Pause = 7,
        Warp = 8
    }

    PlayerControls controls;

    // Triggers to determine if input has changed
    int[] trigger;

    // 0: not pressed, 1: released, 2: pressed down, 3: held down
    int[] input;

    public bool GetInput(Input i) {
        return input[(int)i] > 1;
    }

    public bool GetInput(Input i1, Input i2) {
        return GetInput(i1) && !GetInput(i2);
    }

    public bool GetInputDown(Input i) {
        return input[(int)i] == 2;
    }

    public bool GetInputDown(Input i1, Input i2) {
        return GetInputDown(i1) && !GetInputDown(i2);
    } 

    public bool GetInputUp(Input i) {
        return input[(int)i] == 1;
    }

    public bool GetInputUp(Input i1, Input i2) {
        return GetInputUp(i1) && !GetInputUp(i2);
    }

    void OnEnable() {
        controls.Gameplay.Enable();
    }

    void OnDisable() {
        controls.Gameplay.Disable();
    }

    void Awake() {
        controls = new PlayerControls();

        controls.Gameplay.Up.performed += context => OnUpPerformed();
        controls.Gameplay.Up.canceled += context => OnUpCanceled();
        controls.Gameplay.Down.performed += context => OnDownPerformed();
        controls.Gameplay.Down.canceled += context => OnDownCanceled();
        controls.Gameplay.Left.performed += context => OnLeftPerformed();
        controls.Gameplay.Left.canceled += context => OnLeftCanceled();
        controls.Gameplay.Right.performed += context => OnRightPerformed();
        controls.Gameplay.Right.canceled += context => OnRightCanceled();
        controls.Gameplay.Jump.performed += context => OnJumpPerformed();
        controls.Gameplay.Jump.canceled += context => OnJumpCanceled();
        controls.Gameplay.Spin.performed += context => OnSpinPerformed();
        controls.Gameplay.Spin.canceled += context => OnSpinCanceled();
        controls.Gameplay.Super.performed += context => OnSuperPerformed();
        controls.Gameplay.Super.canceled += context => OnSuperCanceled();
        controls.Gameplay.Pause.performed += context => OnPausePerformed();
        controls.Gameplay.Pause.canceled += context => OnPauseCanceled();
        controls.Gameplay.Warp.performed += context => OnWarpPerformed();
        controls.Gameplay.Warp.canceled += context => OnWarpCanceled();

        trigger = new int[System.Enum.GetNames(typeof(Input)).Length];
        input = new int[System.Enum.GetNames(typeof(Input)).Length];
        
        for(int i = 0; i < System.Enum.GetNames(typeof(Input)).Length; i++) {
            trigger[i] = 0;
            input[i] = 0;
        }
    }

    void Update() {
        for(int i = 0; i < System.Enum.GetNames(typeof(Input)).Length; i++) {
            if(input[i] < 2 && trigger[i] == 1) {
                input[i] = 2;
            }
            else if(input[i] > 1 && trigger[i] == -1) {
                input[i] = 1;
            }
            else if(input[i] == 2) {
                input[i] = 3;
            }
            else if(input[i] == 1) {
                input[i] = 0;
            }
            trigger[i] = 0;
        }

    }

    void OnUpPerformed() {
        trigger[(int)Input.Up] = 1;
    }

    void OnUpCanceled() {
        trigger[(int)Input.Up] = -1;
    }

    void OnDownPerformed() {
        trigger[(int)Input.Down] = 1;
    }

    void OnDownCanceled() {
        trigger[(int)Input.Down] = -1;
    }

    void OnLeftPerformed() {
        trigger[(int)Input.Left] = 1;
    }

    void OnLeftCanceled() {
        trigger[(int)Input.Left] = -1;
    }

    void OnRightPerformed() {
        trigger[(int)Input.Right] = 1;
    }

    void OnRightCanceled() {
        trigger[(int)Input.Right] = -1;
    }

    void OnJumpPerformed() {
        trigger[(int)Input.Jump] = 1;
    }

    void OnJumpCanceled() {
        trigger[(int)Input.Jump] = -1;
    }

    void OnSpinPerformed() {
        trigger[(int)Input.Spin] = 1;
    }

    void OnSpinCanceled() {
        trigger[(int)Input.Spin] = -1;
    }

    void OnSuperPerformed() {
        trigger[(int)Input.Super] = 1;
    }

    void OnSuperCanceled() {
        trigger[(int)Input.Super] = -1;
    }

    void OnPausePerformed() {
        trigger[(int)Input.Pause] = 1;
    }

    void OnPauseCanceled() {
        trigger[(int)Input.Pause] = -1;
    }

    void OnWarpPerformed() {
        trigger[(int)Input.Warp] = 1;
    }

    void OnWarpCanceled() {
        trigger[(int)Input.Warp] = -1;
    }
}