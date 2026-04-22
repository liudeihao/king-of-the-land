using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>选中个体时主相机在 XY 上平滑跟随（不缩放、不改 orthographicSize），[V] 开关注视锁定。</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollowSelection : MonoBehaviour
    {
        [SerializeField] private float _smooth = 5f;
        [SerializeField] private bool _followWhenSelected = true;
        private SelectionManager _sel;

        public void SetSelection(SelectionManager sel) => _sel = sel;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V)) _followWhenSelected = !_followWhenSelected;
        }

        private void LateUpdate()
        {
            if (_sel == null) return;
            if (!_followWhenSelected) return;
            var a = _sel.Selected;
            if (a == null) return;
            var t = a.transform;
            var p = transform.position;
            var z = p.z;
            var target = new Vector3(t.position.x, t.position.y, z);
            var udt = Time.unscaledDeltaTime;
            var k = 1f - Mathf.Exp(-_smooth * udt);
            transform.position = Vector3.Lerp(p, target, k);
        }
    }
}
