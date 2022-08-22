using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using Radkii.Dialogue;
using Radkii.ScriptMethods;
using TMPro;

public class DialogueExample : MonoBehaviour
{
    [SerializeField] private TrieAnimator trieAnimator;
    [SerializeField] private TextMeshProUGUI dialogueText, characterNameText;

    // Start is called before the first frame update
    void Start()
    {
        trieAnimator.characters.Add(new Character("Alice",
            line => { dialogueText.text = line; }, //This action is invoked on every dialogue line
            thisCharacter => { characterNameText.text = "Alice"; }, //This action is invoked when this character starts talking
            lastCharacter => { } //This action is invoked when this character stops talking
        ));
        trieAnimator.characters.Add(new Character("Bob",
            line => { dialogueText.text = line; },
            thisCharacter => { characterNameText.text = "Bob"; },
            lastCharacter => { }
        ));

        trieAnimator.customMethods.Add(new ScriptMethod<string>("ChangeBackgroundColor",
            param =>
			{
                Camera.main.backgroundColor = UnityEngine.Random.ColorHSV();
			}
        ));
    }

    // Update is called once per frame
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Space))
		{
            trieAnimator.NextInDialogue();
		}
    }
}
