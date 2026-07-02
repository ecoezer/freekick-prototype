// Unity 6000.5 uyumu: orijinal dosya obsolete-error veren EndNameEditAction kullanıyordu.
// Yalnızca PostProcessVolumeEditor'ın ihtiyaç duyduğu API korunarak yeniden yazıldı.
using System.IO;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.PostProcessing
{
    public static class ProfileFactory
    {
        public static PostProcessProfile CreatePostProcessProfile(Scene scene, string targetName)
        {
            string path;

            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name + "_Profiles";
                var profilePath = scenePath + "/" + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                    AssetDatabase.CreateFolder(scenePath, extPath);

                path = profilePath + "/";
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = UnityEngine.ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }
    }
}
