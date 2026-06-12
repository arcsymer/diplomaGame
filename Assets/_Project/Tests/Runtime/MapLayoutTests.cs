using System.Collections;
using DiplomaGame.Runtime.Economy;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты редизайна карты v9 (RebuildMapLayout).
    ///
    /// Тесты воспроизводят геометрию, расставленную RebuildMapLayout,
    /// и проверяют ключевые свойства NavMesh и расстановки объектов —
    /// без загрузки Sandbox-сцены (паттерн проекта: тесты создают
    /// свою геометрию; загрузка сцены требует предварительного батч-запуска
    /// и не имеет прецедента в проекте).
    /// </summary>
    [TestFixture]
    public class MapLayoutTests
    {
        // ----------------------------------------------------------------
        // Инфраструктура: земля + NavMesh + маркеры баз v9
        // ----------------------------------------------------------------

        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        // Позиции баз v9
        private static readonly Vector3 PlayerBase = new Vector3(-35f, 0f, -35f);
        private static readonly Vector3 EnemyBase  = new Vector3( 35f, 0f,  35f);

        [SetUp]
        public void SetUp()
        {
            // Большая плоскость: Plane 10×10 ед × scale 10 = 100×100 ед.
            // scale 10 → 100 ед., но базы на ±35 → нужна минимум 80×80 ед.
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "MapLayoutTestGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(10f, 1f, 10f);

            _surface = _groundGo.AddComponent<NavMeshSurface>();
        }

        [TearDown]
        public void TearDown()
        {
            NavMesh.RemoveAllNavMeshData();

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("MapLayoutTest") ||
                    go.name.StartsWith("MapChoke")      ||
                    go.name.StartsWith("MapExpand")     ||
                    go.name.StartsWith("MapPlayer")     ||
                    go.name.StartsWith("MapEnemy"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные фабрики
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт скалу-чокпоинт как BoxCollider-куб (аналог статичного объекта из Forge)
        /// и запекает NavMesh с препятствием.
        /// </summary>
        private static GameObject CreateChokeRock(string goName, Vector3 worldPos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = goName;
            go.transform.position   = worldPos;
            go.transform.localScale = scale;
            return go;
        }

        private static ResourceNode CreateExpandNode(string goName, Vector3 worldPos, int reserve)
        {
            var go = new GameObject(goName);
            go.transform.position = worldPos;
            var node = go.AddComponent<ResourceNode>();
            node.InitForTest(reserve);
            return node;
        }

        // ================================================================
        // Тест 1: PathBetweenBases_Exists
        // ================================================================

        [UnityTest]
        public IEnumerator PathBetweenBases_Exists()
        {
            // Arrange: расставляем 6 скал-чокпоинт с реальными позициями v9
            const float chokeX = 8f;
            var chokeScale = new Vector3(2.5f, 3f, 2.5f);
            float[] chokeZ = { 2f, 0f, -2f };

            foreach (float zVal in chokeZ)
            {
                CreateChokeRock($"MapChoke_L_{zVal}", new Vector3(-chokeX, 0f, zVal), chokeScale);
                CreateChokeRock($"MapChoke_R_{zVal}", new Vector3( chokeX, 0f, zVal), chokeScale);
            }

            // Запекаем NavMesh с препятствиями
            _surface.BuildNavMesh();

            yield return null; // один кадр для применения физики

            // Маркеры баз
            var playerBaseGo = new GameObject("MapPlayerBaseSpawn");
            playerBaseGo.transform.position = PlayerBase;
            var enemyBaseGo = new GameObject("MapEnemyBaseSpawn");
            enemyBaseGo.transform.position = EnemyBase;

            // Act: CalculatePath между позициями маркеров
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(PlayerBase, EnemyBase, NavMesh.AllAreas, path);

            // Assert
            Assert.IsTrue(found && path.status == NavMeshPathStatus.PathComplete,
                $"NavMesh.CalculatePath между PlayerBase={PlayerBase} и EnemyBase={EnemyBase} " +
                "должен возвращать PathComplete. Маршрут обходит чокпоинт по флангу или через проход.");
        }

        // ================================================================
        // Тест 2: ExpandNodes_OnNavMeshAndExtractable
        // ================================================================

        [UnityTest]
        public IEnumerator ExpandNodes_OnNavMeshAndExtractable()
        {
            // Arrange: простая плоскость без препятствий
            _surface.BuildNavMesh();

            yield return null;

            const int expectedReserve = 2000;
            var expandPlayerNode = CreateExpandNode("MapExpandNode_Player",
                new Vector3(-12f, 0f, -18f), expectedReserve);
            var expandEnemyNode  = CreateExpandNode("MapExpandNode_Enemy",
                new Vector3( 12f, 0f,  18f), expectedReserve);

            // Act & Assert — Player node
            bool playerOnMesh = NavMesh.SamplePosition(
                expandPlayerNode.transform.position, out _, 3f, NavMesh.AllAreas);
            Assert.IsTrue(playerOnMesh,
                "ExpandNode_Player (-12,0,-18) должна находиться на NavMesh (SamplePosition в радиусе 3).");
            Assert.IsNotNull(expandPlayerNode.GetComponent<ResourceNode>(),
                "ExpandNode_Player должна иметь компонент ResourceNode.");
            Assert.AreEqual(expectedReserve, expandPlayerNode.Remaining,
                $"ExpandNode_Player.Remaining должен быть {expectedReserve}.");
            Assert.Greater(expandPlayerNode.ExtractUpTo(100), 0,
                "ExpandNode_Player должна быть добываемой (ExtractUpTo > 0).");

            // Assert — Enemy node
            bool enemyOnMesh = NavMesh.SamplePosition(
                expandEnemyNode.transform.position, out _, 3f, NavMesh.AllAreas);
            Assert.IsTrue(enemyOnMesh,
                "ExpandNode_Enemy (+12,0,+18) должна находиться на NavMesh (SamplePosition в радиусе 3).");
            Assert.IsNotNull(expandEnemyNode.GetComponent<ResourceNode>(),
                "ExpandNode_Enemy должна иметь компонент ResourceNode.");
            Assert.AreEqual(expectedReserve, expandEnemyNode.Remaining,
                $"ExpandNode_Enemy.Remaining должен быть {expectedReserve}.");
        }

        // ================================================================
        // Тест 3: ChokeObstacles_BlockCenter
        // ================================================================

        [UnityTest]
        public IEnumerator ChokeObstacles_BlockCenter()
        {
            // Arrange: плоскость + 6 скал-чокпоинт
            const float chokeX = 8f;
            var chokeScale = new Vector3(2.5f, 3f, 2.5f);
            float[] chokeZ = { 2f, 0f, -2f };

            foreach (float zVal in chokeZ)
            {
                CreateChokeRock($"MapChoke_L_{zVal}", new Vector3(-chokeX, 0f, zVal), chokeScale);
                CreateChokeRock($"MapChoke_R_{zVal}", new Vector3( chokeX, 0f, zVal), chokeScale);
            }

            _surface.BuildNavMesh();

            yield return null;

            // Act & Assert: позиция центра скалы Choke_L2 (x=-8, z=0) с радиусом 0.5
            // должна либо не найти точку на NavMesh, либо вернуть точку,
            // смещённую от центра скалы (т.е. сама скала вне NavMesh).
            var chokeLCenter = new Vector3(-chokeX, 0f, 0f);
            bool hitInside = NavMesh.SamplePosition(chokeLCenter, out var hitL, 0.5f, NavMesh.AllAreas);

            // Если точка найдена, она должна быть смещена — центр скалы вне NavMesh
            if (hitInside)
            {
                float displacement = Vector3.Distance(hitL.position, chokeLCenter);
                Assert.Greater(displacement, 0.1f,
                    "Найденная точка на NavMesh должна быть смещена от центра скалы (центр вне NavMesh).");
            }
            else
            {
                // Ещё лучше: точка вообще не найдена внутри скалы
                Assert.IsFalse(hitInside,
                    "Центр скалы Choke_L (x=-8,z=0) не должен находиться на NavMesh — " +
                    "скала является навигационным препятствием.");
            }

            // Также проверяем правую скалу
            var chokeRCenter = new Vector3(chokeX, 0f, 0f);
            bool hitRInside = NavMesh.SamplePosition(chokeRCenter, out var hitR, 0.5f, NavMesh.AllAreas);

            if (hitRInside)
            {
                float displacement = Vector3.Distance(hitR.position, chokeRCenter);
                Assert.Greater(displacement, 0.1f,
                    "Найденная точка на NavMesh должна быть смещена от центра скалы Choke_R.");
            }
        }
    }
}
