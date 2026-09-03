using UnityEngine;

[ExecuteAlways]
public class AvatarFaceCamera : MonoBehaviour
{
    [Header("Camera Target")]
    [Tooltip("Leave empty to automatically track the Main Camera")]
    public Camera targetCamera;

    [Header("Rotation Constraints")]
    [Tooltip("Keeps the avatar upright by only rotating around the Y-axis")]
    public bool onlyRotateY = true;

    [Tooltip("Enable if the model faces backwards")]
    public bool invertDirection = false;

    private void LateUpdate()
    {
        FaceTargetCamera();
    }

    public void FaceTargetCamera()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 targetPos = cam.transform.position;

        if (onlyRotateY)
        {
            targetPos.y = transform.position.y;
        }

        Vector3 lookDir = targetPos - transform.position;

        if (invertDirection)
        {
            lookDir = -lookDir;
        }

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
