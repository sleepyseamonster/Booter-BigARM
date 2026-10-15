using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Authored starting view; the menu and Inspector can request another view for this session.</summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DCameraSelection : MonoBehaviour
    {
        [Tooltip("The view requested at the beginning of every Play session. Runtime changes are temporary.")]
        public TopDown3DCameraView startingView = TopDown3DCameraView.ThirdPerson;
        public TopDown3DElevatedCameraSettings elevated;
        public TopDown3DThirdPersonCameraSettings thirdPerson;
        [SerializeField] private TopDown3DCameraRig outputRig;
        public TopDown3DCameraRig OutputRig => outputRig;
        public TopDown3DCameraOptions Options => OutputRig != null ? OutputRig.CameraOptions : null;
        public bool IsConfigured => outputRig != null && outputRig.gameObject.scene == gameObject.scene
            && elevated != null && thirdPerson != null && thirdPerson.profile != null
            && elevated.transform.IsChildOf(transform) && thirdPerson.transform.IsChildOf(transform);

        private void Start()
        {
            if (IsConfigured && OutputRig != null) OutputRig.EnableCameraOptions();
        }
        public void RequestView(TopDown3DCameraView view)
        {
            if (Options != null) Options.RequestView(view);
        }
    }
}
