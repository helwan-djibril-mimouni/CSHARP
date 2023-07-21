using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Text messageText;

    Pokemon pkmn;

    public void Init(Pokemon pokemon)
    {
        pkmn = pokemon;
        UpdateData();
        SetMessage("");

        pkmn.OnHPChanged += UpdateData;
    }

    void UpdateData()
    {
        nameText.text = pkmn.Base.Name;
        levelText.text = "Lvl " + pkmn.Level;
        hpBar.SetHP((float)pkmn.HP / pkmn.MaxHP);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            nameText.color = GlobalSettings.i.HighlightedColor;
        }
        else
        {
            nameText.color = Color.black;
        }
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }
}
