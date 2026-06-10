using System.Collections;
using DiplomaGame.Runtime.Commands;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Визуальный фидбек приказа: маркер в точке приказа, исчезает за 0.6с.
    /// Пул из 4 маркеров — нет аллокаций при частых приказах.
    /// Подписывается на CommandInput.OrderIssued.
    /// </summary>
    public sealed class OrderMarkerFeedback : MonoBehaviour
    {
        [SerializeField] private CommandInput commandInput;
        [SerializeField] private Material     moveMaterial;
        [SerializeField] private Material     attackMoveMaterial;

        private const int   PoolSize     = 4;
        private const float AnimDuration = 0.6f;

        private GameObject[] _pool;
        private Coroutine[]  _coroutines;   // активные корутины по индексу пула
        private int          _nextIndex;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            BuildPool();
        }

        private void OnEnable()
        {
            if (commandInput != null)
                commandInput.OrderIssued += OnOrderIssued;
        }

        private void OnDisable()
        {
            if (commandInput != null)
                commandInput.OrderIssued -= OnOrderIssued;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(CommandInput input)
        {
            commandInput = input;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void BuildPool()
        {
            _pool       = new GameObject[PoolSize];
            _coroutines = new Coroutine[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "OrderMarker_" + i.ToString();
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);

                // Удаляем коллайдер (маркер не должен блокировать выделение)
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Object.Destroy(col);

                // Назначаем материал по умолчанию
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && moveMaterial != null)
                    mr.sharedMaterial = moveMaterial;

                go.SetActive(false);
                _pool[i] = go;
            }
        }

        private void OnOrderIssued(Vector3 point, UnitCommandType type)
        {
            int idx    = _nextIndex;
            _nextIndex = (_nextIndex + 1) % PoolSize;

            var marker = _pool[idx];

            // Останавливаем предыдущую анимацию этого слота (по Coroutine-дескриптору)
            if (_coroutines[idx] != null)
            {
                StopCoroutine(_coroutines[idx]);
                _coroutines[idx] = null;
            }

            marker.SetActive(false);
            marker.transform.position = point + Vector3.up * 0.05f;

            // Материал по типу
            var mr = marker.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                bool isAttack = type == UnitCommandType.AttackMove;
                var  mat      = isAttack && attackMoveMaterial != null ? attackMoveMaterial : moveMaterial;
                if (mat != null)
                    mr.sharedMaterial = mat;
            }

            _coroutines[idx] = StartCoroutine(PlayMarker(marker));
        }

        private IEnumerator PlayMarker(GameObject marker)
        {
            marker.SetActive(true);

            var   mr         = marker.GetComponent<MeshRenderer>();
            float elapsed    = 0f;
            var   startScale = new Vector3(1.5f, 0.05f, 1.5f);
            var   endScale   = new Vector3(0.5f, 0.05f, 0.5f);

            // Берём начальный цвет материала (для fade через MaterialPropertyBlock без new-аллокаций)
            var block = new MaterialPropertyBlock();

            Color startColor = Color.white;
            if (mr != null && mr.sharedMaterial != null)
                startColor = mr.sharedMaterial.color;

            while (elapsed < AnimDuration)
            {
                float t = elapsed / AnimDuration;
                marker.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                // Fade alpha через PropertyBlock
                if (mr != null)
                {
                    var c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    block.SetColor("_BaseColor", c);
                    mr.SetPropertyBlock(block);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            marker.SetActive(false);
        }
    }
}
