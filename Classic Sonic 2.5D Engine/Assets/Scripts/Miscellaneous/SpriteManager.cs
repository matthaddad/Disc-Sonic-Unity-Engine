using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteManager : MonoBehaviour {

    [Header("Components")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    [Header("Flags")]
    public bool facesCamera = true;
    public bool flipsWithCamera = true;
    public bool destroyAfterAnimation = false;

    string currentSpriteSet;

    string currentAnim;

    string queuedAnimation;
    float queuedSpeed;

    float targetAngle = -1.0f;
    float currentAngle = 0.0f;
    float angleSmoothSpeed = 10.0f;

    bool isFacingRight = false;

    Vector3 planeNormalVector = Vector3.forward;

    SpriteManager spriteAnchor;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void LateUpdate() {
        if(targetAngle >= 0.0f && currentAngle != targetAngle) {
            if(currentAngle < 180.0f) {
                if(currentAngle < targetAngle && targetAngle <= currentAngle + 180.0f) {
                    currentAngle += angleSmoothSpeed;
                    if(!(currentAngle < targetAngle && targetAngle <= currentAngle + 180.0f)) {
                        currentAngle = targetAngle;
                    }
                }
                else {
                    currentAngle -= angleSmoothSpeed;
                    if(currentAngle < targetAngle && targetAngle <= currentAngle + 180.0f) {
                        currentAngle = targetAngle;
                    }
                }
            }
            else {
                if(currentAngle - 180.0f <= targetAngle && targetAngle < currentAngle) {
                    currentAngle -= angleSmoothSpeed;
                    if(!(currentAngle - 180.0f <= targetAngle && targetAngle < currentAngle)) {
                        currentAngle = targetAngle;
                    }
                }
                else {
                    currentAngle += angleSmoothSpeed;
                    if(currentAngle - 180.0f <= targetAngle && targetAngle < currentAngle) {
                        currentAngle = targetAngle;
                    }
                }
            }
        }
        SetAnimationAngle(currentAngle * 0.01745329f, overrideSmooth: false);

        if(spriteAnchor != null) {
            transform.forward = spriteAnchor.gameObject.transform.forward;
            spriteRenderer.flipX = spriteAnchor.spriteRenderer.flipX;
        }
        else {
            if(facesCamera) {
                transform.forward = Camera.main.transform.forward;
            }
            if(flipsWithCamera) {
                bool cameraIsFlipped = Math.Sign(Vector3.Dot(planeNormalVector, Camera.main.transform.forward)) > 0.0f;
                spriteRenderer.flipX = !isFacingRight ^ !cameraIsFlipped;
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, !cameraIsFlipped ? currentAngle : 360.0f - currentAngle);
            }
            else {
                spriteRenderer.flipX = !isFacingRight;
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, currentAngle);
            }
        }

        if(destroyAfterAnimation && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1) {
            Destroy(gameObject);
        }

        if(queuedAnimation != "" && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1) {
            PlayAnimation(queuedAnimation, queuedSpeed);
            queuedAnimation = "";
        }
    }

    public void PlayAnimation(string anim, float speed = 1.5f, float offset = 0.0f) {
        SetAnimationSpeed(speed);
        animator.Play(currentSpriteSet + "-" + anim, -1, offset);
        currentAnim = anim;
        queuedAnimation = "";
    }

    public void PlayAnimation(string anim, string nextAnim, float speed = 1.5f, float nextSpeed = 1.5f, float offset = 0.0f) {
        PlayAnimation(anim, speed, offset);
        queuedAnimation = nextAnim;
        queuedSpeed = nextSpeed;
    }

    public void PlayAnimationWithOffset(string anim, float speed = 1.5f, float offset = 0.0f) {
        PlayAnimation(anim, speed, (offset + animator.GetCurrentAnimatorStateInfo(0).normalizedTime) % 1.0f);
    }

    public void PlayAnimationWithReverseOffset(string anim, float speed = 1.5f, float offset = 0.0f) {
        PlayAnimation(anim, speed, (1.0f - (offset + animator.GetCurrentAnimatorStateInfo(0).normalizedTime)) % 1.0f);
    }

    public float GetAnimationSpeed() {
        return animator.speed;
    }

    public void SetAnimationSpeed(float speed) {
        animator.speed = speed;
    }

    public float GetAnimationAngle() {
        return ((currentAngle / 57.2957795f) + 6.28318531f) % 6.28318531f;
    }

    public void SetAnimationAngle(float angle, bool overrideSmooth = true) {
        currentAngle = (((angle * 57.2957795f) % 360.0f) + 360.0f) % 360.0f;
        if(overrideSmooth) {
            targetAngle = currentAngle;
        }
    }

    public void SetAnimationAngleSmooth(float angle, float smoothSpeed = 10.0f) {
        targetAngle = (((angle * 57.2957795f) % 360.0f) + 360.0f) % 360.0f;
        angleSmoothSpeed = smoothSpeed;
    }

    public bool GetFlipAnimation() {
        return isFacingRight;
    }

    public void FlipAnimation(bool facingRight) {
        isFacingRight = !facingRight;
    }

    public string GetAnimationName() {
        return currentAnim;
    }

    public string GetSpriteSet() {
        return currentSpriteSet;
    }

    public void SetSpriteSet(string spriteSet) {
        currentSpriteSet = spriteSet;
    }

    public bool GetSpriteVisibility() {
        return spriteRenderer.enabled;
    }

    public void SetSpriteVisibility(bool isVisible) {
        spriteRenderer.enabled = isVisible;
    }

    public Vector3 GetNormalVector() {
        return planeNormalVector;
    }

    public void SetNormalVector(Vector3 normalVector) {
        planeNormalVector = normalVector;
    }

    public SpriteManager GetSpriteAnchor() {
        return spriteAnchor;
    }

    public void SetSpriteAnchor(SpriteManager anchor) {
        spriteAnchor = anchor;
    }
}