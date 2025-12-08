using System;
using UnityEngine;

namespace Game.InteractionModules
{
    // Note: Not actually interactable
    public class MonigoteModule : InteractableModule
    {
        [SerializeField] private PlayerMovement  playerMovement;
        
        public override void UpdateState(ModuleData data)
        {
            if (data.ValueTypes.HasFlag(SentDataTypes.Float))
                playerMovement.movementSpeed = data.floatValue*200;
            if (data.ValueTypes.HasFlag(SentDataTypes.Bool) && data.boolValue)
            {
                // Not definitive behaviour
                MovementState newState = MovementState.Idle;
                switch (playerMovement.GetState())
                {
                    case MovementState.Idle: newState = MovementState.MovingLeft; break;
                    case MovementState.MovingLeft: newState = MovementState.Waiting; break;
                    case MovementState.MovingRight: newState = MovementState.Idle; break;
                    case MovementState.Waiting: newState = MovementState.MovingRight; break;
                }
                playerMovement.SetState(newState);
            }
        }
    }
}