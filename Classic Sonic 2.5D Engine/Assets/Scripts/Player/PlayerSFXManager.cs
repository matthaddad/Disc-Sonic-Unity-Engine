using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class PlayerSFXManager : MonoBehaviour {

    [Header("Components")]
    AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip noneSFX;
    public AudioClip skidSFX;
    public AudioClip spinSFX;
    public AudioClip spindashSFX;
    public AudioClip dashreleaseSFX;
    public AudioClip jumpSFX;
    public AudioClip dropdashSFX;
    public AudioClip peeloutSFX;
    public AudioClip peeloutreleaseSFX;
    public AudioClip instashieldSFX;
    public AudioClip flySFX;
    public AudioClip flytiredSFX;
    public AudioClip glideslideSFX;
    public AudioClip glidelandSFX;
    public AudioClip wallclingSFX;
    public AudioClip getblueshieldSFX;
    public AudioClip getbubbleshieldSFX;
    public AudioClip bubblebounceSFX;
    public AudioClip getfireshieldSFX;
    public AudioClip firedashSFX;
    public AudioClip getlightningshieldSFX;
    public AudioClip lightningjumpSFX;

    void Awake() {
        audioSource = GetComponent<AudioSource>();
    }

    void OnApplicationQuit() {
        StopSFX();
        PlayOneShotSFX(noneSFX);
    }

    public void PlaySFX(AudioClip SFX, bool loop = false, float pitch = 1.0f) {
        audioSource.clip = SFX;
        audioSource.loop = loop;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    public void StopSFX() {
        audioSource.clip = noneSFX;
        audioSource.Play();
    }

    public void PlayOneShotSFX(AudioClip SFX, float pitch = 1.0f) {
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(SFX);
        audioSource.PlayOneShot(noneSFX);
    }
}