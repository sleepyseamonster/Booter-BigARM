using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        private InputActionAsset ownedAnimationActions, authoredAnimationActions;
        private InputAction animationJump, animationCrouch, animationAttack, animationCancel, animationPalette, animationWalk;
        private int animationRequests;
        private bool animationAwaitNeutral=true;
        public bool AnimationSlowWalkHeld {get;private set;}
        public bool HasAnimationCommands => animationPalette != null;
        public Func<bool> AnimationActionOwned;
        public event Action AnimationJumpRequested, AnimationCrouchRequested, AnimationActionRequested,
            AnimationCancelRequested, AnimationPaletteRequested;

        private void PrepareAnimationInput()
        {
            if(!Application.isPlaying || inputActions == null || ownedAnimationActions != null || TopDown3DAnimationPlaygroundSession.Find(gameObject.scene)==null) return;
            authoredAnimationActions=inputActions;
            ownedAnimationActions=AnimationPlaygroundInputFactory.Create(inputActions);
            inputActions=ownedAnimationActions;
        }
        private void BindAnimationCommands()
        {
            if(!AnimationPlaygroundInputFactory.IsLabAsset(inputActions)) return;
            animationJump=inputActions.FindAction("Gameplay/Jump",true);
            animationCrouch=inputActions.FindAction("Gameplay/Crouch",true);
            animationAttack=inputActions.FindAction("Gameplay/Attack",true);
            animationCancel=inputActions.FindAction("Gameplay/AnimationCancel",true);
            animationPalette=inputActions.FindAction(AnimationPlaygroundInputFactory.PalettePath,true);
            animationWalk=inputActions.FindAction("Gameplay/AnimationSlowWalk",true);
            foreach(var action in new[]{animationJump,animationCrouch,animationAttack,animationCancel,animationPalette}) action.performed+=CaptureAnimationCommand;
            DisarmAnimationCommands();
        }
        private void UnbindAnimationCommands()
        {
            foreach(var action in new[]{animationJump,animationCrouch,animationAttack,animationCancel,animationPalette})
                if(action != null) action.performed-=CaptureAnimationCommand;
            animationJump=animationCrouch=animationAttack=animationCancel=animationPalette=animationWalk=null;
            DisarmAnimationCommands();
        }
        private void CaptureAnimationCommand(InputAction.CallbackContext context)
        {
            if(animationAwaitNeutral || !CanCaptureGameplayCommand) return;
            animationRequests|=context.action == animationCancel ? 1 : context.action == animationPalette ? 2
                : context.action == animationCrouch ? 4 : context.action == animationJump ? 8 : 16;
        }
        private bool ResolveAnimationCommands(bool modalRequested)
        {
            var requests=animationRequests;animationRequests=0;
            if(!HasAnimationCommands) return false;
            if(modalRequested || !CanCaptureGameplayCommand)
            {DisarmAnimationCommands();return false;}
            if(animationAwaitNeutral)
            {
                if(!animationJump.IsPressed() && !animationCrouch.IsPressed() && !animationAttack.IsPressed()
                    && !animationCancel.IsPressed() && !animationPalette.IsPressed() && !animationWalk.IsPressed()) animationAwaitNeutral=false;
                return false;
            }
            AnimationSlowWalkHeld=animationWalk.IsPressed();
            if((requests&1)!=0 && AnimationActionOwned?.Invoke()==true){AnimationCancelRequested?.Invoke();return true;}
            if((requests&2)!=0){AnimationPaletteRequested?.Invoke();return true;}
            if((requests&4)!=0){AnimationCrouchRequested?.Invoke();return true;}
            if((requests&8)!=0){AnimationJumpRequested?.Invoke();return true;}
            if((requests&16)!=0){AnimationActionRequested?.Invoke();return true;}
            return false;
        }
        private void DisarmAnimationCommands(){animationRequests=0;animationAwaitNeutral=true;AnimationSlowWalkHeld=false;}
        public void ClearAnimationCommandEdges()=>DisarmAnimationCommands();
        private void ReleaseAnimationAsset()
        {
            if(ownedAnimationActions==null) return;
            if(inputActions==ownedAnimationActions) inputActions=authoredAnimationActions;
            if(Application.isPlaying) Destroy(ownedAnimationActions);else DestroyImmediate(ownedAnimationActions);
            ownedAnimationActions=null;authoredAnimationActions=null;
        }
        private void OnDestroy(){UnbindAnimationCommands();ReleaseAnimationAsset();}
    }
}
