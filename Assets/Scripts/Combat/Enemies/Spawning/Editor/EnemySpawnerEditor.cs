using UnityEngine;
using UnityEditor;
using SickOfIt.Combat.Enemies.Spawning;

namespace SickOfIt.Combat.Enemies.Spawning.Editor
{
    [CustomEditor(typeof(EnemySpawner))]
    public class EnemySpawnerEditor : UnityEditor.Editor
    {
        private EnemySpawner spawner;
        
        private void OnEnable()
        {
            spawner = (EnemySpawner)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Show wave info at runtime
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime Wave Info", MessageType.Info);
                
                SerializedProperty wavesProperty = serializedObject.FindProperty("waves");
                SerializedProperty currentWaveIndexProperty = serializedObject.FindProperty("currentWaveIndex");
                SerializedProperty enemiesSpawnedInWaveProperty = serializedObject.FindProperty("enemiesSpawnedInWave");
                SerializedProperty totalEnemiesSpawnedProperty = serializedObject.FindProperty("totalEnemiesSpawned");
                SerializedProperty aliveEnemiesProperty = serializedObject.FindProperty("aliveEnemies");
                
                int currentWave = currentWaveIndexProperty.intValue;
                int totalWaves = wavesProperty.arraySize;
                
                EditorGUILayout.LabelField("Current Wave:", $"{currentWave + 1} / {totalWaves}");
                EditorGUILayout.LabelField("Spawned in Wave:", enemiesSpawnedInWaveProperty.intValue.ToString());
                EditorGUILayout.LabelField("Total Spawned:", totalEnemiesSpawnedProperty.intValue.ToString());
                EditorGUILayout.LabelField("Alive Enemies:", aliveEnemiesProperty.arraySize.ToString());
                
                Repaint();
            }
        }

        private void OnSceneGUI()
        {
            if (spawner == null)
                return;

            // Get serialized properties for spawn bounds
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty minBounds = so.FindProperty("spawnBoundsMin");
            SerializedProperty maxBounds = so.FindProperty("spawnBoundsMax");
            
            Vector2 min = minBounds.vector2Value;
            Vector2 max = maxBounds.vector2Value;
            Vector3 spawnerPos = spawner.transform.position;
            
            // Calculate corners of the spawn bounds
            Vector3 bottomLeft = spawnerPos + new Vector3(min.x, min.y, 0);
            Vector3 bottomRight = spawnerPos + new Vector3(max.x, min.y, 0);
            Vector3 topLeft = spawnerPos + new Vector3(min.x, max.y, 0);
            Vector3 topRight = spawnerPos + new Vector3(max.x, max.y, 0);
            
            // Draw the spawn zone
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[] { bottomLeft, bottomRight, topRight, topLeft },
                new Color(1f, 0.3f, 0.3f, 0.1f),
                new Color(1f, 0.3f, 0.3f, 0.8f)
            );
            
            // Draw center point
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(spawnerPos, Vector3.forward, 0.2f);
            
            // Draw corner handles for resizing
            EditorGUI.BeginChangeCheck();
            
            // Min corner (bottom-left)
            var fmh_84_17_639066014281996940 = Quaternion.identity; Vector3 newBottomLeft = Handles.FreeMoveHandle(
                bottomLeft,
                0.15f,
                Vector3.zero,
                Handles.DotHandleCap
            );
            
            // Max corner (top-right)
            var fmh_93_17_639066014282010552 = Quaternion.identity; Vector3 newTopRight = Handles.FreeMoveHandle(
                topRight,
                0.15f,
                Vector3.zero,
                Handles.DotHandleCap
            );
            
            // Additional corners for easier editing
            var fmh_102_17_639066014282012146 = Quaternion.identity; Vector3 newBottomRight = Handles.FreeMoveHandle(
                bottomRight,
                0.15f,
                Vector3.zero,
                Handles.DotHandleCap
            );
            
            var fmh_110_17_639066014282013140 = Quaternion.identity; Vector3 newTopLeft = Handles.FreeMoveHandle(
                topLeft,
                0.15f,
                Vector3.zero,
                Handles.DotHandleCap
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spawner, "Modify Spawn Bounds");
                
                // Update bounds based on which handle was moved
                Vector3 offset;
                
                // Bottom-left handle
                offset = newBottomLeft - bottomLeft;
                if (offset.sqrMagnitude > 0.001f)
                {
                    min.x += offset.x;
                    min.y += offset.y;
                }
                
                // Top-right handle
                offset = newTopRight - topRight;
                if (offset.sqrMagnitude > 0.001f)
                {
                    max.x += offset.x;
                    max.y += offset.y;
                }
                
                // Bottom-right handle
                offset = newBottomRight - bottomRight;
                if (offset.sqrMagnitude > 0.001f)
                {
                    max.x += offset.x;
                    min.y += offset.y;
                }
                
                // Top-left handle
                offset = newTopLeft - topLeft;
                if (offset.sqrMagnitude > 0.001f)
                {
                    min.x += offset.x;
                    max.y += offset.y;
                }
                
                // Ensure min is always less than max
                if (min.x > max.x)
                {
                    float temp = min.x;
                    min.x = max.x;
                    max.x = temp;
                }
                if (min.y > max.y)
                {
                    float temp = min.y;
                    min.y = max.y;
                    max.y = temp;
                }
                
                minBounds.vector2Value = min;
                maxBounds.vector2Value = max;
                so.ApplyModifiedProperties();
            }
            
            // Draw labels
            Handles.color = Color.white;
            Vector3 center = spawnerPos + new Vector3((min.x + max.x) / 2f, (min.y + max.y) / 2f, 0);
            Handles.Label(center, $"Spawn Zone\n{max.x - min.x:F1} x {max.y - min.y:F1}");
            
            // Draw wave info if in play mode
            if (Application.isPlaying)
            {
                SerializedProperty currentWaveIndexProperty = so.FindProperty("currentWaveIndex");
                SerializedProperty wavesProperty = so.FindProperty("waves");
                
                int currentWave = currentWaveIndexProperty.intValue;
                int totalWaves = wavesProperty.arraySize;
                
                Handles.Label(spawnerPos + Vector3.up * 1.5f, $"Wave {currentWave + 1}/{totalWaves}");
            }
        }
    }
}
