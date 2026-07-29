using UnityEngine;

[ExecuteAlways]
public class AvatarLightFollower : MonoBehaviour
{
    public string avatarName = "HA";
    public Vector3 localOffset;
    public bool parentToAvatar = true;

    private Transform avatarTransform;

    private void OnEnable()
    {
        FollowAvatar();
    }

    private void LateUpdate()
    {
        FollowAvatar();
    }

    private void OnValidate()
    {
        FollowAvatar();
    }

    private void FollowAvatar()
    {
        if (avatarTransform == null)
        {
            GameObject avatar = GameObject.Find(avatarName);
            if (avatar == null)
            {
                return;
            }

            avatarTransform = avatar.transform;
        }

        if (parentToAvatar && transform.parent != avatarTransform)
        {
            transform.SetParent(avatarTransform, false);
        }

        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
    }
}
