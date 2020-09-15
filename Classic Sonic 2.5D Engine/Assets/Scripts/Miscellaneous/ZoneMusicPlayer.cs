using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneMusicPlayer : MonoBehaviour {

	public AudioSource musicSource; 

	public AudioClip zoneMusicA;
	public AudioClip zoneMusicB;
	public AudioClip speedupMusic;
	public AudioClip invincibleMusic;
	public AudioClip drowningMusic;
	public AudioClip superMusicA;
	public AudioClip superMusicB;

    // Start is called before the first frame update
    void Start() {
    	musicSource.clip = zoneMusicB;
    	musicSource.playOnAwake = false;
    	musicSource.loop = true;
        musicSource.PlayOneShot(zoneMusicA);
        musicSource.PlayScheduled(AudioSettings.dspTime + zoneMusicA.length);
    }
}
