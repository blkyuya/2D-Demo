using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    // LateUpdate 保证角色 Update 里的移动先执行，镜头最后跟，避免画面抖动
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        // Lerp 让镜头有惯性感，停下后仍会缓缓滑到目标位，比直接赋值更丝滑
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
}
