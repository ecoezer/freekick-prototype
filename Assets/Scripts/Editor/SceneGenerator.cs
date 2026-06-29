using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneGenerator
{
    public static void GenerateScene()
    {
        Debug.Log("[SceneGenerator] Starting scene generation...");
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Directional Light
        GameObject lightObj = new GameObject("DirectionalLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // 2. Pitch (Ground)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Pitch";
        ground.transform.localScale = new Vector3(68, 1, 105);
        ground.transform.position = new Vector3(0, -0.5f, 0);
        Material greenMat = new Material(Shader.Find("Standard"));
        greenMat.color = new Color(0.2f, 0.6f, 0.2f);
        ground.GetComponent<MeshRenderer>().material = greenMat;

        // 3. Goal
        GameObject goal = new GameObject("Goal");
        goal.transform.position = new Vector3(0, 0, 35);
        
        GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPost.name = "LeftPost";
        leftPost.transform.SetParent(goal.transform);
        leftPost.transform.localPosition = new Vector3(-3.66f, 1.22f, 0);
        leftPost.transform.localScale = new Vector3(0.12f, 2.44f, 0.12f);

        GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPost.name = "RightPost";
        rightPost.transform.SetParent(goal.transform);
        rightPost.transform.localPosition = new Vector3(3.66f, 1.22f, 0);
        rightPost.transform.localScale = new Vector3(0.12f, 2.44f, 0.12f);

        GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossbar.name = "Crossbar";
        crossbar.transform.SetParent(goal.transform);
        crossbar.transform.localPosition = new Vector3(0, 2.44f, 0);
        crossbar.transform.localScale = new Vector3(7.44f, 0.12f, 0.12f);

        // 4. Ball
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0, 0.11f, 0);
        ball.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.45f;
        
        BallLauncher launcher = ball.AddComponent<BallLauncher>();
        BallStateTracker tracker = ball.AddComponent<BallStateTracker>();
        BallResetter resetter = ball.AddComponent<BallResetter>();
        GoalDirectionProvider dirProvider = ball.AddComponent<GoalDirectionProvider>();
        ShotInput input = ball.AddComponent<ShotInput>();
        ShotController controller = ball.AddComponent<ShotController>();
        BallPhysicsController physicsController = ball.AddComponent<BallPhysicsController>();

        // We need to use SerializedObject to set private serialized fields, 
        // or we can make them public. Since they are private [SerializeField], we use SerializedObject.
        SerializedObject physControllerSO = new SerializedObject(physicsController);
        physControllerSO.FindProperty("ballLauncher").objectReferenceValue = launcher;
        physControllerSO.ApplyModifiedProperties();
        SerializedObject dirProviderSO = new SerializedObject(dirProvider);
        dirProviderSO.FindProperty("goalCenter").objectReferenceValue = goal.transform;
        dirProviderSO.ApplyModifiedProperties();

        SerializedObject inputSO = new SerializedObject(input);
        inputSO.FindProperty("ballTransform").objectReferenceValue = ball.transform;
        inputSO.FindProperty("directionProviderObject").objectReferenceValue = dirProvider;
        inputSO.ApplyModifiedProperties();

        SerializedObject trackerSO = new SerializedObject(tracker);
        trackerSO.FindProperty("ballLauncher").objectReferenceValue = launcher;
        trackerSO.ApplyModifiedProperties();

        SerializedObject resetterSO = new SerializedObject(resetter);
        resetterSO.FindProperty("ballStateTracker").objectReferenceValue = tracker;
        resetterSO.FindProperty("ballLauncher").objectReferenceValue = launcher;
        resetterSO.ApplyModifiedProperties();

        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("shotInput").objectReferenceValue = input;
        controllerSO.FindProperty("ballLauncher").objectReferenceValue = launcher;
        controllerSO.FindProperty("ballResetter").objectReferenceValue = resetter;
        controllerSO.ApplyModifiedProperties();

        // 5. Camera
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        FreekickCameraController camController = camObj.AddComponent<FreekickCameraController>();

        SerializedObject camControllerSO = new SerializedObject(camController);
        camControllerSO.FindProperty("ballTransform").objectReferenceValue = ball.transform;
        camControllerSO.FindProperty("ballLauncher").objectReferenceValue = launcher;
        camControllerSO.FindProperty("ballResetter").objectReferenceValue = resetter;
        camControllerSO.ApplyModifiedProperties();

        // Save scene
        string scenePath = "Assets/Scenes/Main.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        // Add scene to Build Settings
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[1];
        buildScenes[0] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = buildScenes;

        Debug.Log("[SceneGenerator] Scene generation completed and saved to " + scenePath);
    }
}
