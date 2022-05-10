using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CombatController : MonoBehaviour
{
    public static bool ActionsLocked = false;
    public static CombatController _this;

    [Header("Current combat/movement actions and items")]
    public CombatActions currentAction = CombatActions.None;
    public GameObject currentInterface;
    public BattleUnits AttackUnit;
    public int currentAbility;

    [Header("Combo UI elements")]
    public GameObject AllBottom;
    public GameObject AllTop;

    [Header("UI Elements to enable/disable")]
    public GameObject AttackButton;
    public GameObject CancelAttackButton;
    public GameObject AbilityGroup;
    public GameObject ConfirmGroup;
    public GameObject ConfirmGroupCancel;

    private void Awake()
    {
        _this = this;
    }

    /// <summary>
    /// This will trigger when a cancel button is pressed on UI
    /// </summary>
    public void Cancel()
    {
        ResetAllStates();
    }

    /// <summary>
    /// This will trigger when a confirmation button is pressed on UI
    /// </summary>
    public void Confirm()
    {
        switch (currentAction)
        {
            case CombatActions.Skip:
                HideAllSelectors();
                DisableBottom();
                TurnController._this.CompleteTurn();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// This will trigger once the attack button is pressed on UI
    /// </summary>
    public void DoAttack()
    {
        ResetAllStates();
        EnableCancel(CombatActions.Attack);
        ShowTargetIndicator(SkillRange.NormalAttack);
    }

    /// <summary>
    /// This triggers when the ability button is pressed on UI
    /// </summary>
    /// <param name="AbilityID">Ability to trigger</param>
    public void DoAbility(int AbilityID)
    {
        ResetAllStates();
        HideAllButtons();
        _this.CancelAttackButton.SetActive(true);
        currentAction = CombatActions.Ability;

        currentAbility = AbilityID;
    }

    /// <summary>
    /// Triggers when skip button is pressed
    /// </summary>
    public void DoSkip()
    {
        ResetAllStates();
        HideAllButtons();

        _this.ConfirmGroup.SetActive(true);
        currentAction = CombatActions.Skip;
    }

    /// <summary>
    /// Triggers when switch button is pressed
    /// </summary>
    public void DoSwitch()
    {
        ResetAllStates();
        EnableCancel(CombatActions.Switch);

        SwitchController._this.DisplaySwitch(TurnController._this.CurTeam);
    }


    /// <summary>
    /// This will reset the combat to prepare for a next turn or complete any action
    /// </summary>
    public void ResetAllStates()
    {
        SwitchController._this.ResetSwitch();
        _this.ConfirmGroupCancel.SetActive(true);
        _this.AllBottom.SetActive(true);
        _this.AllTop.SetActive(true);
        _this.AttackButton.SetActive(true);
        _this.AbilityGroup.SetActive(true);

        _this.CancelAttackButton.SetActive(false);
        _this.ConfirmGroup.SetActive(false); 

        // Reset current action and selected ability
        currentAction = CombatActions.None;
        currentAbility = 0;

        HideAllSelectors();
    }

    /// <summary>
    /// Hide all buttons on bottom
    /// </summary>
    private void HideAllButtons()
    {
        _this.AttackButton.SetActive(false);
        _this.CancelAttackButton.SetActive(false);
        _this.AbilityGroup.SetActive(false);
        _this.ConfirmGroup.SetActive(false);
    }

    /// <summary>
    /// Disable bottom group
    /// </summary>
    public void DisableBottom()
    {
        _this.AllBottom.SetActive(false);
        _this.AllTop.SetActive(false);
    }

    /// <summary>
    /// Show bottom group
    /// </summary>
    public void EnableBottom()
    {
        _this.AllBottom.SetActive(true);
        _this.AllTop.SetActive(true);
    }

    /// <summary>
    /// Show cancel button
    /// </summary>
    /// <param name="action"></param>
    private void EnableCancel(CombatActions action)
    {
        _this.AttackButton.SetActive(false);
        _this.CancelAttackButton.SetActive(true);
        currentAction = action;
    }

    /// <summary>
    /// Show selected targets on battlefield
    /// </summary>
    /// <param name="selection"></param>
    private void ShowTargetIndicator(SkillRange selection)
    {
        BattleUnits Unit = CombatSetUnits.GetUnit(TurnController._this.CurTeam, TurnController._this.CurTurn);
        int Range = Unit.UnitData.attackRange;

        switch (selection)
        {
            case SkillRange.NormalAttack:
                for (int Position = 1; Position <= Range; Position++)
                {
                    BattleUnits OtherUnit = CombatSetUnits.GetUnit((TurnController._this.CurTeam == 1 ? 2 : 1), Position);

                    if (OtherUnit != null && !OtherUnit.unitState.isDead && TurnController._this.CurTeam == 1)
                    {
                        OtherUnit.UnitObject.GetComponent<UnitComponents>().SelectorMonster.SetActive(true);
                        OtherUnit.UnitObject.GetComponent<UnitComponents>().OvalSelection.SetActive(true);
                    }
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Hide all target selectors on battlefield
    /// </summary>
    public static void HideAllSelectors()
    {
        GameObject[] Indicators;
        Indicators = GameObject.FindGameObjectsWithTag("SelectorObjects");

        foreach (GameObject indicator in Indicators)
            indicator.SetActive(false);

        GameObject[] Ovals;
        Ovals = GameObject.FindGameObjectsWithTag("Ovals");

        foreach (GameObject Oval in Ovals)
            Oval.SetActive(false);
    }

    /// <summary>
    /// This will trigger once the unit is selected on battlefield
    /// </summary>
    /// <param name="Team"></param>
    /// <param name="Position"></param>
    public void OnTargetTouch(int Team, int Position)
    {
        HideAllSelectors();
        DisableBottom();

        if (ActionsLocked)
            return;

        BattleUnits MyUnit = CombatSetUnits.GetUnit(TurnController._this.CurTeam, TurnController._this.CurTurn);
        BattleUnits EnemyUnit = CombatSetUnits.GetUnit(Team, Position);

        switch (currentAction)
        {
            case CombatActions.Attack:
                PerformNormalAttack(MyUnit, EnemyUnit);
                break;
            case CombatActions.Switch:
                SwitchController._this.SwitchStart(MyUnit, Team, Position);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// This will trigger after unit is selected and combat action is attack
    /// </summary>
    /// <param name="MyUnit"></param>
    /// <param name="EnemyUnit"></param>
    public void PerformNormalAttack(BattleUnits MyUnit, BattleUnits EnemyUnit)
    {
        CombatSetUnits._this.SelectCameras(MyUnit, EnemyUnit);
        MyUnit.SaveLastPosition();
        MyUnit.currentAction = CombatActions.Attack;
        lockActions();

        for (int i = 0; i < MyUnit.UnitData.Abilities.Count; i++)
        {
            if (MyUnit.UnitData.Abilities[i].Ability.skillType == SkillType.NormalAttack) // Find normal attack
                MyUnit.Behaviors[i].StartAttack(EnemyUnit);
        }
    }

    /// <summary>
    /// Show only some units
    /// </summary>
    /// <param name="MyUnit"></param>
    /// <param name="EnemyUnit"></param>
    public void HideOtherUnits(BattleUnits MyUnit, BattleUnits EnemyUnit)
    {
        for (int i = 0; i < CombatSetUnits._this.battleUnits.Count; i++)
        {
            CombatSetUnits._this.battleUnits[i].UnitObject.SetActive(false);
        }

        MyUnit.UnitObject.SetActive(true);
        EnemyUnit.UnitObject.SetActive(true);
    }

    /// <summary>
    /// Show units again
    /// </summary>
    public void ShowAllUnits()
    {
        for (int i = 0; i < CombatSetUnits._this.battleUnits.Count; i++)
            CombatSetUnits._this.battleUnits[i].UnitObject.SetActive(true);

    }

    /// <summary>
    /// Lock actions of combat
    /// </summary>
    private void lockActions()
    {
        ActionsLocked = true;
    }
}
