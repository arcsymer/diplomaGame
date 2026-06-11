using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для UnitAnimator.
    /// Проверяют null-safe поведение: компонент без Animator не бросает исключений,
    /// а также базовые контракты (инстанцирование, lifecycle).
    /// Горячий путь Update (velocity → IsMoving) и подписки на события
    /// верифицируются в PlayMode (UnitAnimatorTests).
    /// </summary>
    [TestFixture]
    public class UnitAnimatorLogicTests
    {
        // ----------------------------------------------------------------
        // Null-safe: UnitAnimator без Animator не бросает исключений
        // ----------------------------------------------------------------

        [Test]
        public void UnitAnimator_WithoutAnimator_DoesNotThrow()
        {
            // Arrange: минимальный GameObject с Unit + NavMeshAgent — без Animator
            var go = new GameObject("TestUnit_NoAnimator");
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            go.AddComponent<Unit>();
            go.AddComponent<DiplomaGame.Runtime.Combat.Health>();

            // Act & Assert: AddComponent не бросает и компонент включается
            Assert.DoesNotThrow(() =>
            {
                var ua = go.AddComponent<UnitAnimator>();
                Assert.IsNotNull(ua, "UnitAnimator должен добавиться без исключений.");
            });

            Object.DestroyImmediate(go);
        }

        // ----------------------------------------------------------------
        // Animator.StringToHash: параметры должны быть хэшируемы без ошибок
        // ----------------------------------------------------------------

        [Test]
        public void AnimatorParameters_HashValues_AreNonZero()
        {
            // Проверяем, что имена параметров аниматора не пустые и хэшируются корректно
            int isMovingHash = Animator.StringToHash("IsMoving");
            int attackHash   = Animator.StringToHash("Attack");
            int dieHash      = Animator.StringToHash("Die");

            Assert.AreNotEqual(0, isMovingHash, "Hash 'IsMoving' не должен быть 0.");
            Assert.AreNotEqual(0, attackHash,   "Hash 'Attack' не должен быть 0.");
            Assert.AreNotEqual(0, dieHash,      "Hash 'Die' не должен быть 0.");

            // Параметры должны быть различными
            Assert.AreNotEqual(isMovingHash, attackHash, "'IsMoving' и 'Attack' должны иметь разные хэши.");
            Assert.AreNotEqual(isMovingHash, dieHash,    "'IsMoving' и 'Die' должны иметь разные хэши.");
            Assert.AreNotEqual(attackHash,   dieHash,    "'Attack' и 'Die' должны иметь разные хэши.");
        }

        // ----------------------------------------------------------------
        // UnitAnimator присутствует в namespace DiplomaGame.Runtime.Units
        // ----------------------------------------------------------------

        [Test]
        public void UnitAnimator_IsInCorrectNamespace()
        {
            var type = typeof(UnitAnimator);
            Assert.AreEqual("DiplomaGame.Runtime.Units", type.Namespace,
                "UnitAnimator должен находиться в namespace DiplomaGame.Runtime.Units.");
        }

        // ----------------------------------------------------------------
        // UnitAnimator наследует MonoBehaviour
        // ----------------------------------------------------------------

        [Test]
        public void UnitAnimator_InheritsMonoBehaviour()
        {
            Assert.IsTrue(
                typeof(MonoBehaviour).IsAssignableFrom(typeof(UnitAnimator)),
                "UnitAnimator должен наследовать MonoBehaviour.");
        }
    }
}
