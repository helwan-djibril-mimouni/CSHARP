using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoxPartySlotUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text lvlText;
    [SerializeField] Image image;

    public void SetData(Pokemon pokemon)
    {
        nameText.text = pokemon.Base.Name;
        lvlText.text = "" + pokemon.Level;
        if (pokemon.Shiny)
        {
            image.sprite = pokemon.Base.ShinyFrontSprite;
        }
        else
        {
            image.sprite = pokemon.Base.FrontSprite;
        }
        image.color = new Color(255, 255, 255, 100);
    }

    public void ClearData()
    {
        nameText.text = "";
        lvlText.text = "";
        image.sprite = null;
        image.color = new Color(255, 255, 255, 0);
    }
}
