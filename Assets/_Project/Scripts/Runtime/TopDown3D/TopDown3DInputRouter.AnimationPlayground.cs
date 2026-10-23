using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        private InputActionAsset ownedAnimationActions, authoredAnimationActions;
        private InputAction animationJump, animationCrouch, animationAttack, animationCancel, animationPalette, animationWalk, animationDodge;
        private int animationRequests;
        private bool animationAwaitNeutral=true;
        private int animationAwaitRelease=63;
        private bool animationWalkAwaitRelease=true;
        private InputDevice animationFacingDevice, animationCommandDevice;
        public bool AnimationSlowWalkHeld {get;private set;}
        public bool HasAnimationCommands => animationPalette != null;
        public bool LaboratoryStandingFacingDisarmed => HasAnimationCommands && animationAwaitNeutral;
        public bool LaboratoryStandingFacingHeld => HasAnimationCommands && !animationAwaitNeutral && Mode == TopDown3DInputMode.Gameplay
            && !LaboratoryResetBlocked && CameraLookAheadHeld;
        public bool LaboratoryResetBlocked { get; private set; }
        private bool CanCaptureAnimationPalette => Mode == TopDown3DInputMode.Gameplay
            && inputUpdateMode == TopDown3DInputMode.Gameplay && (!Application.isPlaying || Application.isFocused);
        public Func<bool> AnimationActionOwned;
        public event Action AnimationJumpRequested, AnimationCrouchRequested, AnimationActionRequested,
            AnimationCancelRequested, AnimationPaletteRequested, AnimationDodgeRequested;

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
            animationDodge=inputActions.FindAction("Gameplay/AnimationDodge",true);
            foreach(var action in new[]{animationJump,animationCrouch,animationAttack,animationCancel,animationPalette,animationDodge}) action.performed+=CaptureAnimationCommand;
            cameraLookAheadAction.performed += CaptureAnimationFacingDevice;
            InputSystem.onDeviceChange += HandleAnimationFacingDeviceLoss;
            DisarmAnimationCommands();
        }
        private void UnbindAnimationCommands()
        {
            if (cameraLookAheadAction != null) cameraLookAheadAction.performed -= CaptureAnimationFacingDevice;
            InputSystem.onDeviceChange -= HandleAnimationFacingDeviceLoss;
            foreach(var action in new[]{animationJump,animationCrouch,animationAttack,animationCancel,animationPalette,animationDodge})
                if(action != null) action.performed-=CaptureAnimationCommand;
            animationJump=animationCrouch=animationAttack=animationCancel=animationPalette=animationWalk=animationDodge=null;
            DisarmAnimationCommands();
        }
        private void CaptureAnimationFacingDevice(InputAction.CallbackContext context) => animationFacingDevice = context.control.device;
        private void HandleAnimationFacingDeviceLoss(InputDevice device, InputDeviceChange change)
        {
            if (device != animationFacingDevice && device != animationCommandDevice || change != InputDeviceChange.Removed
                && change != InputDeviceChange.Disconnected && change != InputDeviceChange.Disabled) return;
            CameraLookAheadHeld = false; DisarmAnimationCommands();
        }
        private void CaptureAnimationCommand(InputAction.CallbackContext context)
        {
            var request=AnimationRequestBit(context.action);
            if((animationAwaitRelease&request)!=0 || !(context.action == animationPalette ? CanCaptureAnimationPalette : CanCaptureGameplayCommand)) return;
            animationCommandDevice=context.control.device;
            animationRequests|=request;
        }
        private int AnimationRequestBit(InputAction action)=>action==animationDodge ? 32 : action==animationCancel ? 1 : action==animationPalette ? 2
            : action==animationCrouch ? 4 : action==animationJump ? 8 : 16;
        private void RearmReleasedAnimationCommands()
        {
            if(!animationCancel.IsPressed())animationAwaitRelease&=~1;
            if(!animationPalette.IsPressed())animationAwaitRelease&=~2;
            if(!animationCrouch.IsPressed())animationAwaitRelease&=~4;
            if(!animationJump.IsPressed())animationAwaitRelease&=~8;
            if(!animationAttack.IsPressed())animationAwaitRelease&=~16;
            if(!animationDodge.IsPressed())animationAwaitRelease&=~32;
            if(!animationWalk.IsPressed())animationWalkAwaitRelease=false;
            if(cameraLookAheadAction?.IsPressed()!=true)animationAwaitNeutral=false;
        }
        private bool ResolveAnimationCommands(bool modalRequested)
        {
            var requests=animationRequests;animationRequests=0;
            if(!HasAnimationCommands) return false;
            if(modalRequested || !CanCaptureAnimationPalette)
            {DisarmAnimationCommands();return false;}
            // Each command needs its own release after an interruption. A held
            // facing/walk modifier or another action must not suppress fresh buttons.
            RearmReleasedAnimationCommands();
            AnimationSlowWalkHeld=!animationWalkAwaitRelease && animationWalk.IsPressed();
            if((requests&1)!=0 && AnimationActionOwned?.Invoke()==true){AnimationCancelRequested?.Invoke();return true;}
            if((requests&2)!=0){AnimationPaletteRequested?.Invoke();return true;}
            if(LaboratoryResetBlocked) return true;
            if((requests&4)!=0){AnimationCrouchRequested?.Invoke();return true;}
            if((requests&32)!=0){AnimationDodgeRequested?.Invoke();return true;}
            if((requests&8)!=0){AnimationJumpRequested?.Invoke();return true;}
            if((requests&16)!=0){AnimationActionRequested?.Invoke();return true;}
            return false;
        }
        public event Action AnimationCommandsDisarmed;
        private void DisarmAnimationCommands(){animationRequests=0;animationAwaitNeutral=true;animationAwaitRelease=63;animationWalkAwaitRelease=true;AnimationSlowWalkHeld=false;animationFacingDevice=animationCommandDevice=null;AnimationCommandsDisarmed?.Invoke();}
        public void ClearAnimationCommandEdges()=>DisarmAnimationCommands();
        public void ClearLaboratoryIntent()
        {
            ClearGameplayIntent(); DisarmAnimationCommands();
            awaitMoveNeutral=moveAction != null && moveAction.ReadValue<Vector2>().sqrMagnitude > .0484f;
            awaitLookNeutral=lookAction != null && lookAction.ReadValue<Vector2>().sqrMagnitude > .0484f;
        }
        public void SetLaboratoryResetBlocked(bool blocked)
        { LaboratoryResetBlocked=blocked; ClearLaboratoryIntent(); }
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
