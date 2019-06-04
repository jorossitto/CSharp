using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{
    Dictionary<Character, GameObject> characterGameObjectMap;
    Dictionary<string, Sprite> characterSprites;
    const string CHARACTERSORTINGLAYER = "Characters";

    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();
        //Instantiate our dictionary that tracks which gameobject is rending which tile data
        characterGameObjectMap = new Dictionary<Character, GameObject>();
        //Register our callback so our gameobject gets updated whenever the tile's type changes
        world.RegisterCharacterCreated(OnCharacterCreated);

        //check for preexisting characters, which won't do the callback.
        foreach(Character character in world.characters)
        {
            OnCharacterCreated(character);
        }
    }

    private void LoadSprites()
    {
        characterSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Characters");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            characterSprites[s.name] = s;
        }
    }

    public void OnCharacterCreated(Character character)
    {
        //Create a visual gameobject linked to this data
        //Debug.Log("OnCharacterCreated");

        //Todo FixMe does not consider multi tile objects or rotated objects

        //Creates a new gameobject and adds it to our scene
        GameObject characterGameObject = new GameObject();
        //Add tile//Gameobject to dictionary
        characterGameObjectMap.Add(character, characterGameObject);
        characterGameObject.name = "Character" + "(" + character.X + "," + character.Y + ")";
        characterGameObject.transform.position = new Vector3(character.X, character.Y, 0);
        characterGameObject.transform.SetParent(this.transform, true);

        //Add a sprite renderer -> don't bother setting sprite because all tiles are empty
        SpriteRenderer characterSpriteRenderer = characterGameObject.AddComponent<SpriteRenderer>();

        //todo fixme assume that object must be a wall so use the hardcoded refrence to the wall sprite
        characterSpriteRenderer.sprite = characterSprites["chain_armor_2"];
        characterSpriteRenderer.sortingLayerName = CHARACTERSORTINGLAYER;

        //Register our callback so that our gameobject gets updated whenever the object's into changes
        character.RegisterOnChangedCallback(OnCharacterChanged);
    }

    void OnCharacterChanged(Character character)
    {
        //Debug.Log("OnCharacterChanged " + character);

        //Make sure the furniture graphics are correct.

        if (characterGameObjectMap.ContainsKey(character) == false)
        {
            Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map");
            return;
        }

        GameObject characterGameObject = characterGameObjectMap[character];
        characterGameObject.transform.position = new Vector3(character.X, character.Y, 0);

    }

}
