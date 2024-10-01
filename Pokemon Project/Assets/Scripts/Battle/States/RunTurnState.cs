using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class RunTurnState : State<BattleSystem>
{
    public static RunTurnState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    BattleUnit playerUnit;
    BattleUnit enemyUnit;
    BattleDialogBox dialogBox;
    PartyScreen partyScreen;
    bool isTrainerBattle;
    PokemonParty playerParty;
    PokemonParty trainerParty;

    BattleSystem bs;
    public override void Enter(BattleSystem owner)
    {
        bs = owner;

        playerUnit = bs.PlayerUnit;
        enemyUnit = bs.EnemyUnit;
        dialogBox = bs.DialogBox;
        partyScreen = bs.PartyScreen;
        isTrainerBattle = bs.IsTrainerBattle;
        playerParty = bs.PlayerParty;
        trainerParty = bs.TrainerParty;

        StartCoroutine(RunTurns(bs.SelectedAction));
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        if (playerAction == BattleAction.Move)
        {
            bs.PlayerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[bs.SelectedMove];
            bs.EnemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
            {
                playerGoesFirst = false;
            }
            else if (enemyMovePriority == playerMovePriority)
            {
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;
            }

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;

            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (bs.IsBattleOver) yield break;

            if (secondPokemon.HP > 0)
            {
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (bs.IsBattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchPokemon)
            {
                yield return bs.SwitchPokemon(bs.SelectedPokemon);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                if (bs.SelectedItem is PokeballItem)
                {
                    yield return bs.ThrowPokeball(bs.SelectedItem as PokeballItem);
                    if (bs.IsBattleOver) yield break;
                }
            }
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
            }

            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (bs.IsBattleOver) yield break;
        }

        if (!bs.IsBattleOver)
        {
            bs.StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();
                
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.name}");

        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {
            var damageDetails = new DamageDetails();

            sourceUnit.PlayAttackAnimation();
            AudioManager.i.PlaySFX(move.Base.Sound);

            yield return new WaitForSeconds(1f);

            targetUnit.PlayHitAnimation();
            AudioManager.i.PlaySFX(AudioID.Hit);

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.WaitForHPUpdate();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.SecondaryEffects != null && move.Base.SecondaryEffects.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                foreach (var secondary in move.Base.SecondaryEffects)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                    {
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                    }
                }
            }

            yield return RunAfterMove(damageDetails, move.Base, sourceUnit, targetUnit.Pokemon);

            if (targetUnit.Pokemon.HP <= 0)
            {
                yield return HandlePokemonFainted(targetUnit);
            }

        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
    {
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                source.ApplyBoosts(effects.Boosts);
            }
            else
            {
                target.ApplyBoosts(effects.Boosts);
            }
        }

        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterMove(DamageDetails details, MoveBase move, BattleUnit sourceUnit, Pokemon target)
    {
        if (details == null)
            yield break;

        if (move.DrainingPercentage != 0)
        {
            int healedHP = Mathf.Clamp(Mathf.CeilToInt(details.DamageDealt / 100f * move.DrainingPercentage), 1, sourceUnit.Pokemon.MaxHP);
            sourceUnit.Pokemon.IncreaseHP(healedHP);
            yield return sourceUnit.Hud.WaitForHPUpdate();
        }

        if (move.Recoil.recoilType != RecoilType.None)
        {
            int damage = 0;
            switch (move.Recoil.recoilType)
            {
                case RecoilType.RecoilByMaxHP:
                    int maxHp = sourceUnit.Pokemon.MaxHP;
                    damage = Mathf.FloorToInt(maxHp * (move.Recoil.recoilDamage / 100f));
                    sourceUnit.Pokemon.TakeRecoilDamage(damage);
                    break;
                case RecoilType.RecoilByCurrentHP:
                    int currentHp = sourceUnit.Pokemon.HP;
                    damage = Mathf.FloorToInt(currentHp * (move.Recoil.recoilDamage / 100f));
                    sourceUnit.Pokemon.TakeRecoilDamage(damage);
                    break;
                case RecoilType.RecoilByDamage:
                    damage = Mathf.FloorToInt(details.DamageDealt * (move.Recoil.recoilDamage / 100f));
                    sourceUnit.Pokemon.TakeRecoilDamage(damage);
                    break;
                default:
                    UnityEngine.Debug.Log("Error: Unknown Recoil Effect");
                    break;
            }
        }

        //ReUsed from status changes
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (bs.IsBattleOver) yield break;

        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return HandlePokemonFainted(sourceUnit);
        }
    }

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
    {
        if (move.Base.AlwaysHits)
        {
            return true;
        }

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
        {
            moveAccuracy *= boostValues[accuracy];
        }
        else
        {
            moveAccuracy /= boostValues[-accuracy];
        }

        if (evasion > 0)
        {
            moveAccuracy /= boostValues[evasion];
        }
        else
        {
            moveAccuracy *= boostValues[-evasion];
        }

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Pokemon.Base.Name} fainted");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        if (!faintedUnit.IsPlayerUnit)
        {
            bool battleWon = true;
            if (isTrainerBattle)
            {
                battleWon = trainerParty.GetHealthyPokemon() == null;
            }

            if (battleWon)
            {
                AudioManager.i.PlayMusic(bs.BattleVictoryMusic);
            }

            int expYield = faintedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Pokemon.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} exp");
            yield return playerUnit.Hud.SetExpSmooth();

            yield return new WaitForSeconds(1f);

            while (playerUnit.Pokemon.CheckForLevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} grew to level {playerUnit.Pokemon.Level}");

                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrentLevel();
                if (newMove != null)
                {
                    if (playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumOfMoves)
                    {
                        playerUnit.Pokemon.LearnMove(newMove.Base);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Base.Name}");
                        dialogBox.SetMovesNames(playerUnit.Pokemon.Moves);
                    }
                    else
                    {
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} wants to learn {newMove.Base.Name}");
                        yield return dialogBox.TypeDialog($"But it cannot learn more than {PokemonBase.MaxNumOfMoves} moves");
                        yield return dialogBox.TypeDialog($"Choose a move to forget");

                        MoveToForgetState.i.NewMove = newMove.Base;
                        MoveToForgetState.i.CurrentMoves = playerUnit.Pokemon.Moves.Select(m => m.Base).ToList();
                        yield return GameController.Instance.StateMachine.PushAndWait(MoveToForgetState.i);

                        int moveIndex = MoveToForgetState.i.Selection;
                        if (moveIndex == PokemonBase.MaxNumOfMoves || moveIndex == -1)
                        {
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} did not learn {newMove.Base.Name}");
                        }
                        else
                        {
                            var selectedMove = playerUnit.Pokemon.Moves[moveIndex].Base;
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {newMove.Base.Name}");

                            playerUnit.Pokemon.Moves[moveIndex] = new Move(newMove.Base);
                        }
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);
            }
        }

        yield return CheckForBattleOver(faintedUnit);
    }

    IEnumerator CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = playerParty.GetHealthyPokemon();
            if (nextPokemon != null)
            {
                yield return GameController.Instance.StateMachine.PushAndWait(PartyState.i);
                yield return bs.SwitchPokemon(PartyState.i.SelectedPokemon);
            }
            else
            {
                bs.BattleOver(false);
            }
        }
        else
        {
            if (!isTrainerBattle)
            {
                bs.BattleOver(true);
            }
            else
            {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if (nextPokemon != null)
                {
                    AboutToUseState.i.NewPokemon = nextPokemon;
                    yield return bs.StateMachine.PushAndWait(AboutToUseState.i);
                }
                else
                {
                    bs.BattleOver(true);
                }
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog("A critical hit!");
        }

        if (damageDetails.TypeEffectiveness > 1)
        {
            yield return dialogBox.TypeDialog("It's super effective!");
        }
        else if (damageDetails.TypeEffectiveness < 0.01)
        {
            yield return dialogBox.TypeDialog("It has no effect.");
        }
        else if (damageDetails.TypeEffectiveness < 1)
        {
            yield return dialogBox.TypeDialog("It's not very effective...");
        }
    }

    IEnumerator TryToEscape()
    {
        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles !");
            yield break;
        }

        ++bs.EscapeAttempts;

        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely !");
            bs.BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * bs.EscapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely !");
                bs.BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Didn't manage to escape !");
            }
        }
    }
}
