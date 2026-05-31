using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// System that reads from the generated Input System actions and
/// writes the results into the player's PlayerInputData component.
/// Uses SystemBase (required to hold the managed InputSystem_Actions reference)
/// with SystemAPI.Query instead of the obsolete Entities.ForEach.
/// </summary>
public partial class PlayerInputSystem : SystemBase
{
    private InputSystem_Actions _actions;

    protected override void OnCreate()
    {
        base.OnCreate();

        _actions = new InputSystem_Actions();
        _actions.Player.Enable();

        RequireForUpdate<PlayerInputData>();
    }

    protected override void OnDestroy()
    {
        _actions.Player.Disable();
        _actions.Dispose();

        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        Vector2 moveVec = _actions.Player.Move.ReadValue<Vector2>();
        float2 move = new float2(moveVec.x, moveVec.y);
        bool jumped = _actions.Player.Jump.WasPerformedThisFrame();

        foreach (var inputData in SystemAPI.Query<RefRW<PlayerInputData>>())
        {
            inputData.ValueRW.Move = move;
            inputData.ValueRW.Jump = jumped;
        }
    }
}

