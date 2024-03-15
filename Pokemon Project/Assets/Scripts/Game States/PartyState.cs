using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyState : State<GameController>
{
    [SerializeField] PartyScreen partyScreen;

    public Pokemon SelectedPokemon { get; private set; }

    public static PartyState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        SelectedPokemon = null;
        partyScreen.gameObject.SetActive(true);
        partyScreen.OnSelected += OnPokemonSelected;
        partyScreen.OnBack += OnBack;
    }

    public override void Execute()
    {
        partyScreen.HandleUpdate();
    }

    public override void Exit()
    {
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPokemonSelected;
        partyScreen.OnBack -= OnBack;
    }

    void OnPokemonSelected(int selection)
    {
        SelectedPokemon = partyScreen.SelectedMember;

        StartCoroutine(PokemonSelectedAction());
    }

    IEnumerator PokemonSelectedAction()
    {
        var prevState = gc.StateMachine.GetPrevState();
        if (prevState == InventoryState.i)
        {
            StartCoroutine(GoToUseItemState());
        }
        else if (prevState == BattleState.i)
        {
            var battleState = prevState as BattleState;

            DynamicMenuState.i.MenuItems = new List<string>() { "Shift", "Summary", "Cancel" };
            yield return gc.StateMachine.PushAndWait(DynamicMenuState.i);

            if (DynamicMenuState.i.SelectedItem == 0)
            {
                if (SelectedPokemon.HP <= 0)
                {
                    partyScreen.SetMessageText("You can't send out a fainted Pokemon");
                    yield break;
                }
                else if (SelectedPokemon == battleState.BattleSystem.PlayerUnit.Pokemon)
                {
                    partyScreen.SetMessageText("You can't switch with the same Pokemon");
                    yield break;
                }

                gc.StateMachine.Pop();
            }
            else if (DynamicMenuState.i.SelectedItem == 1)
            {
                Debug.Log($"Selected pokemon for summary");
            }
            else
            {
                yield break;
            }
        }
        else
        {
            DynamicMenuState.i.MenuItems = new List<string>() { "Summary", "Switch Position", "Cancel" };
            yield return gc.StateMachine.PushAndWait(DynamicMenuState.i);

            if (DynamicMenuState.i.SelectedItem == 0)
            {
                Debug.Log($"Selected pokemon for summary");
            }
            else if (DynamicMenuState.i.SelectedItem == 1)
            {
                Debug.Log($"Selected pokemon switch position");
            }
            else
            {
                yield break;
            }
        }
    }

    IEnumerator GoToUseItemState()
    {
        yield return gc.StateMachine.PushAndWait(UseItemState.i);
        gc.StateMachine.Pop();
    }

    void OnBack()
    {
        SelectedPokemon = null;

        var prevState = gc.StateMachine.GetPrevState();
        if (prevState == BattleState.i)
        {
            var battleState = prevState as BattleState;
            if (battleState.BattleSystem.PlayerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SetMessageText("You have to send a Pokemon to continue");
                return;
            }
        }

        gc.StateMachine.Pop();
    }
}
