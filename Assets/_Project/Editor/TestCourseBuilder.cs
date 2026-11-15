#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Menü: Tools → Test Sahnesini Kur. Gri test parkurunu otomatik oluşturur.
public static class TestCourseBuilder
{
    [MenuItem("Tools/Test Sahnesini Kur")]
    public static void Build()
    {
        // Eski parkur varsa sor ve sil
        var old = GameObject.Find("TestCourse");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Test sahnesi", "TestCourse zaten var. Silip yeniden kurayım mı?", "Evet", "Hayır"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        // Zemin: kalın bir küp, üst yüzü Y = 0
        var oldGround = GameObject.Find("Ground");
        if (oldGround != null) Undo.DestroyObjectImmediate(oldGround);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(200f, 1f, 200f);
        Undo.RegisterCreatedObjectUndo(ground, "Test sahnesi");

        var root = new GameObject("TestCourse");
        Undo.RegisterCreatedObjectUndo(root, "Test sahnesi");

        // Spawn noktaları
        var spawn1 = Empty(root, "SpawnPoint_1", new Vector3(-1.5f, 1.1f, 0f));
        Empty(root, "SpawnPoint_2", new Vector3(1.5f, 1.1f, 0f));

        // A: Yürünebilir rampa (20°)
        Box(root, "Ramp_20", new Vector3(-12f, 0.95f, 10.8f), new Vector3(4f, 0.2f, 6f), -20f);
        Box(root, "Ramp_20_Top", new Vector3(-12f, 1f, 15.6f), new Vector3(4f, 2f, 4f));

        // B: Çıkılamayan rampa (50°)
        Box(root, "Ramp_50", new Vector3(-6f, 1.5f, 9.3f), new Vector3(4f, 0.2f, 4f), -50f);

        // C: Merdiven (6 basamak, her biri 0.25 m)
        for (int i = 0; i < 6; i++)
        {
            float h = 0.25f * (i + 1);
            Box(root, $"Step_{i + 1}", new Vector3(0f, h / 2f, 8.25f + 0.5f * i), new Vector3(3f, h, 0.5f));
        }
        Box(root, "Stairs_Top", new Vector3(0f, 0.75f, 12.5f), new Vector3(3f, 1.5f, 3f));

        // D: Çukur (2 m boşluk)
        Box(root, "Pit_Start", new Vector3(6f, 0.5f, 10f), new Vector3(4f, 1f, 4f));
        Box(root, "Pit_End", new Vector3(6f, 0.5f, 16f), new Vector3(4f, 1f, 4f));
        Kill(root, "Pit_KillZone", new Vector3(6f, 0.25f, 13f), new Vector3(4f, 0.5f, 2f));

        // E: Dar geçit (1.5 m)
        Box(root, "Wall_Left", new Vector3(11f, 1f, 10f), new Vector3(0.5f, 2f, 4f));
        Box(root, "Wall_Right", new Vector3(13f, 1f, 10f), new Vector3(0.5f, 2f, 4f));

        // Engeller
        Box(root, "Obstacle_1", new Vector3(-3f, 0.5f, -6f), Vector3.one);
        Box(root, "Obstacle_2", new Vector3(2f, 0.5f, -8f), Vector3.one);
        Box(root, "Obstacle_3", new Vector3(0f, 0.5f, -11f), Vector3.one);
        Box(root, "Obstacle_Beam", new Vector3(0f, 2.45f, -14f), new Vector3(4f, 0.3f, 0.3f));

        // Haritadan düşeni yakalayan alan
        Kill(root, "WorldKillZone", new Vector3(0f, -10f, 0f), new Vector3(300f, 1f, 300f));

        // Oyuncunun spawn noktasını bağla
        var respawn = Object.FindFirstObjectByType<PlayerRespawn>();
        if (respawn != null)
        {
            var so = new SerializedObject(respawn);
            so.FindProperty("spawnPoint").objectReferenceValue = spawn1.transform;
            so.ApplyModifiedProperties();
        }

        // Test tepsisini spawn'ın önüne koy
        var tray = Object.FindFirstObjectByType<FakeGrabbable>();
        if (tray != null)
        {
            Undo.RecordObject(tray.transform, "Test sahnesi");
            tray.transform.position = new Vector3(0f, 0.5f, 3f);
        }

        Selection.activeGameObject = root;
        Debug.Log("Test sahnesi kuruldu. Ctrl+S ile kaydetmeyi unutma.");
    }

    private static GameObject Empty(GameObject parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = position;
        return go;
    }

    private static GameObject Box(GameObject parent, string name, Vector3 position, Vector3 scale, float rotationX = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(rotationX, 0f, 0f);
        go.transform.localScale = scale;
        return go;
    }

    private static void Kill(GameObject parent, string name, Vector3 position, Vector3 scale)
    {
        var go = Box(parent, name, position, scale);
        go.GetComponent<BoxCollider>().isTrigger = true;
        go.GetComponent<MeshRenderer>().enabled = false;
        go.AddComponent<KillZone>();
    }
}
#endif