using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    bool wasInitalized = false;
    float soundCooldown = 0;
    const string DEFAULT = "Default";

    // Start is called before the first frame update
    void Start()
    {
        WorldController.Instance.World.RegisterFurnitureCreated(OnFurnitureCreated);
        WorldController.Instance.World.RegisterTileChanged(OnTileChanged);
    }

    // Update is called once per frame
    void Update()
    {
        soundCooldown -= Time.deltaTime;
    }

    void OnTileChanged(Tile tileData)
    {
        //todo fixme
        if(soundCooldown > 0)
        {
            return;
        }

        AudioClip ac = Resources.Load<AudioClip>("Sounds/Tile Placed");
        if (ac == null)
        {
            //No specific sound please use default sound
            ac = Resources.Load<AudioClip>("Sounds/" + DEFAULT);
            Debug.LogError("Sound file does not exist: " + ac);
        }

        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = .1f;
    }
    public void OnFurnitureCreated(Furniture furn)
    {
        //todo fixme
        if (soundCooldown > 0)
        {
            return;
        }

        AudioClip ac = Resources.Load<AudioClip>("Sounds/" + furn.objectType);

        if (ac == null)
        {
            //No specific sound please use default sound
            ac = Resources.Load<AudioClip>("Sounds/" + DEFAULT);
            Debug.LogError("Sound file does not exist: " + ac);
        }
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = .1f;
    }
}
